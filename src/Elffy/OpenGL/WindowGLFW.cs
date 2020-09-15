#nullable enable
using System;
using System.Reflection;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Elffy.AssemblyServices;
using Elffy.OpenGL.Windowing;
using Elffy.Core.OpenToolkit;
using OpenToolkit.Windowing.Common;
using OpenToolkit.Windowing.GraphicsLibraryFramework;
using Monitor = OpenToolkit.Windowing.GraphicsLibraryFramework.Monitor;
using Wnd = OpenToolkit.Windowing.GraphicsLibraryFramework.Window;

namespace Elffy.OpenGL
{
    /// <summary>Raw window class of GLFW</summary>
    internal unsafe sealed partial class WindowGLFW : IDisposable
    {
        const double MaxUpdateFrequency = 500;

        private bool _isCloseRequested;

        private Wnd* _window;
        private Vector2i _clientSize;
        private Vector2i _size;
        private Vector2i _location;
        private string _title;

        private double? _updateFrequency = 60.0;
        private double _updateEpsilon; // 前回のアップデートからの持ちこし誤差 (sec), quantization error for Updating

        private readonly Stopwatch _watchUpdate = new Stopwatch();

        private bool _isLoaded;

        public bool IsDisposed => _window == null;

        /// <summary>Frequency of updating (Hz). If null, update is called as faster as possible.</summary>
        public double? UpdateFrequency
        {
            get => _updateFrequency;
            set
            {
                if(value == null) {
                    _updateFrequency = value;
                }
                else {
                    _updateFrequency = Math.Max(1.0, Math.Min(value.Value, MaxUpdateFrequency));
                }
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                if(IsDisposed) { ThrowDisposed(); }
                value ??= "";
                GLFW.SetWindowTitle(_window, value);
                _title = value;
            }
        }

        public Vector2i ClientSize
        {
            get => _clientSize;
            set
            {
                if(IsDisposed) { ThrowDisposed(); }
                GLFW.SetWindowSize(_window, value.X, value.Y);
            }
        }

        public Vector2i Size => _size;

        public Vector2i Location
        {
            get => _location;
            set
            {
                if(IsDisposed) { ThrowDisposed(); }
                GLFW.SetWindowPos(_window, value.X, value.Y);
            }
        }

        public WindowGLFW() : this(800, 600, "Window", WindowStyle.Default, false, WindowIconRaw.Empty)
        {
        }

        public WindowGLFW(int width, int height, string title, WindowStyle style, bool antiAliased, WindowIconRaw icon)
        {
            title ??= "";

            // [Hard coded setting]
            // - Opengl core profile
            // - Opengl api (not es)
            // - 4.1 or later (4.1 is the last version Mac supports)
            // Vsync is enabled


            // Call GLFWProvider.EnsureInitialized()
            // (That ensures glfwInit())
            OpenTKHelper.GLFWProvider_EnsureInitialized();

            var monitor = GLFW.GetPrimaryMonitor();
            if(monitor == null) {
                throw new InvalidOperationException("This computer has no monitors. GLWF cannot create window.");
            }
            var isFullscreen = false;
            switch(style) {
                case WindowStyle.Default:
                    GLFW.WindowHint(WindowHintBool.Resizable, true);
                    break;
                case WindowStyle.Fullscreen:
                    GLFW.WindowHint(WindowHintBool.Resizable, true);
                    isFullscreen = true;
                    break;
                case WindowStyle.FixedWindow:
                    GLFW.WindowHint(WindowHintBool.Resizable, false);
                    break;
                default:
                    throw new ArgumentException(nameof(style));
            }
            GLFW.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);
            GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.OpenGlApi);
            GLFW.WindowHint(WindowHintInt.ContextVersionMajor, 4);
            GLFW.WindowHint(WindowHintInt.ContextVersionMinor, 1);
            GLFW.WindowHint(WindowHintBool.Focused, true);
            GLFW.WindowHint(WindowHintBool.Visible, false);
            if(antiAliased) {
                GLFW.WindowHint(WindowHintInt.Samples, 4);
            }
            if(isFullscreen) {
                var videoMode = GLFW.GetVideoMode(monitor);
                GLFW.WindowHint(WindowHintInt.RedBits, videoMode->RedBits);
                GLFW.WindowHint(WindowHintInt.GreenBits, videoMode->GreenBits);
                GLFW.WindowHint(WindowHintInt.BlueBits, videoMode->BlueBits);
                //GLFW.WindowHint(WindowHintInt.RefreshRate, videoMode->RefreshRate);
                GLFW.WindowHint(WindowHintInt.RefreshRate, 60);                         // TODO: とりあえず60fps固定
                _window = GLFW.CreateWindow(width, height, title, monitor, null);
            }
            else {
                _window = GLFW.CreateWindow(width, height, title, null, null);
            }

            try {
                GLFW.MakeContextCurrent(_window);
                InitializeGlBindings();
                RegisterWindowCallbacks();
                GLFW.FocusWindow(_window);
                _title = title;
                if(icon.Images.Length != 0) {
                    GLFW.SetWindowIcon(_window, icon.Images);
                }

                GLFW.GetWindowSize(_window, out _clientSize.X, out _clientSize.Y);
                GLFW.GetWindowFrameSize(_window, out var left, out var top, out var right, out var bottom);
                _size = new Vector2i(_clientSize.X + left + right, _clientSize.Y + top + bottom);
                GLFW.GetWindowPos(_window, out _location.X, out _location.Y);

                GLFW.SwapInterval(1);               // Enable Vsync

            }
            catch {
                GLFW.DestroyWindow(_window);
                _window = null;
                GLFW.DefaultWindowHints();
                throw;
            }
            //finally {
            //    GLFW.MakeContextCurrent(null);
            //}
        }

        public void HandleOnce()
        {
            if(_isCloseRequested || IsDisposed) {
                if(IsDisposed == false) {
                    GLFW.MakeContextCurrent(_window);
                    Dispose();
                    GLFW.MakeContextCurrent(null);
                }
                return;
            }

            if(!_watchUpdate.IsRunning) {
                _watchUpdate.Start();
            }

            try {
                GLFW.MakeContextCurrent(_window);
                var updateFrequency = _updateFrequency;

                if(updateFrequency == null) {
                    // Update frame as faster as possible.
                    var elapsedSec = _watchUpdate.Elapsed.TotalSeconds;
                    _watchUpdate.Restart();
                    Update(elapsedSec);
                    return;
                }
                else {
                    // Update frame if enough time elapsed from the last updating.
                    var targetPeriod = 1.0 / updateFrequency.Value;
                    var elapsedSec = _updateEpsilon + _watchUpdate.Elapsed.TotalSeconds;
                    var over = elapsedSec - targetPeriod;
                    if(over >= 0) {
                        _watchUpdate.Restart();
                        _updateEpsilon = over;
                        Update(elapsedSec);
                        return;
                    }
                    else {
                        // If it is too early to update, do only polling events.
                        GLFW.PollEvents();
                        return;
                    }
                }
            }
            finally {
                //GLFW.MakeContextCurrent(null);
            }
            

            void Update(double elapsed)
            {
                GLFW.PollEvents();
                if(IsDisposed) { return; }

                UpdateFrame?.Invoke(this, new FrameEventArgs(elapsed));
                if(IsDisposed) { return; }
            }
        }

        public void SwapBuffers()
        {
            if(IsDisposed) { ThrowDisposed(); }
            GLFW.SwapBuffers(_window);
        }

        public void MakeContextCurrent()
        {
            if(IsDisposed) { ThrowDisposed(); }
            GLFW.MakeContextCurrent(_window);
        }

        public void Show()
        {
            if(IsDisposed) { ThrowDisposed(); }
            GLFW.ShowWindow(_window);
            if(!_isLoaded) {
                _isLoaded = true;
                GLFW.MakeContextCurrent(_window);
                Load?.Invoke(this);
                //GLFW.MakeContextCurrent(null);
            }
        }

        public void Hide()
        {
            if(IsDisposed) { ThrowDisposed(); }
            GLFW.HideWindow(_window);
        }

        private unsafe void OnClientSizeResized(int width, int height)
        {
            GLFW.SetWindowSize(_window, width, height);
            GLFW.GetFramebufferSize(_window, out width, out height);
            ClientSize = new Vector2i(width, height);
        }

        private static void InitializeGlBindings()
        {
            // This method must be called for each gl context created.

            var provider = new GLFWBindingsContext();
            try {
                var assembly = Assembly.Load("OpenToolkit.Graphics");

                LoadBindings("ES11");
                LoadBindings("ES20");
                LoadBindings("ES30");
                LoadBindings("OpenGL");
                LoadBindings("OpenGL4");

                void LoadBindings(string typeNamespace)
                {
                    assembly
                        !.GetType("OpenToolkit.Graphics." + typeNamespace + ".GL")
                        !.GetMethod("LoadBindings")
                        !.Invoke(null, new object[1] { provider! });
                }
            }
            catch {
                if(AssemblyState.IsDebug) {
                    throw;
                }
            }
        }

        public void Dispose()
        {
            if(IsDisposed) { return; }  // Block re-entrant
            var window = _window;
            _window = null;
            Closed?.Invoke(this);
            GLFW.DestroyWindow(window);
        }

        [DoesNotReturn]
        private void ThrowDisposed() => throw new ObjectDisposedException(nameof(WindowGLFW), "This window is already disposed.");
    }
}
