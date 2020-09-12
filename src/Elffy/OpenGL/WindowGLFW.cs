#nullable enable
using OpenToolkit.Windowing.Common;
using OpenToolkit.Windowing.Common.Input;
using OpenToolkit.Windowing.GraphicsLibraryFramework;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Elffy.AssemblyServices;

using Monitor = OpenToolkit.Windowing.GraphicsLibraryFramework.Monitor;
using Wnd = OpenToolkit.Windowing.GraphicsLibraryFramework.Window;
using Image = OpenToolkit.Windowing.GraphicsLibraryFramework.Image;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Elffy.Core.OpenToolkit;
using System.Runtime.CompilerServices;

namespace Elffy.OpenGL
{
    /// <summary>Raw window class of GLFW</summary>
    internal unsafe sealed class WindowGLFW : IDisposable
    {
        const double MaxUpdateFrequency = 500;

        private Wnd* _window;
        private Vector2i _clientSize;
        private Vector2i _size;
        private Vector2i _location;
        private string _title;

        private double? _updateFrequency = 60.0;
        private double _updateEpsilon; // 前回のアップデートからの持ちこし誤差 (sec), quantization error for Updating

        private readonly Stopwatch _watchUpdate = new Stopwatch();

        private bool _isLoaded;


        public event Action<WindowGLFW>? Load;

        //public event Action<WindowGLFW>? Unload;

        public event Action<WindowGLFW, FrameEventArgs>? UpdateFrame;

        //public event Action<WindowGLFW, FrameEventArgs>? RenderFrame;

        public event Action<WindowGLFW, WindowPositionEventArgs>? Move;
        public event Action<WindowGLFW, ResizeEventArgs>? Resize;

        public event Action<WindowGLFW>? Refresh;

        public event Action<WindowGLFW, CancelEventArgs>? Closing;
        public event Action<WindowGLFW>? Closed;

        public event Action<WindowGLFW, MinimizedEventArgs>? Minimized;

        public event Action<WindowGLFW, JoystickEventArgs>? JoystickConnected;

        public event Action<WindowGLFW, FocusedChangedEventArgs>? FocusedChanged;

        public event Action<WindowGLFW, TextInputEventArgs>? TextInput;

        public event Action<WindowGLFW, KeyboardKeyEventArgs>? KeyDown;
        public event Action<WindowGLFW, KeyboardKeyEventArgs>? KeyUp;

        public event Action<WindowGLFW, MonitorEventArgs>? MonitorConnected;

        public event Action<WindowGLFW>? MouseLeave;
        public event Action<WindowGLFW>? MouseEnter;
        public event Action<WindowGLFW, MouseButtonEventArgs>? MouseDown;
        public event Action<WindowGLFW, MouseButtonEventArgs>? MouseUp;
        public event Action<WindowGLFW, MouseMoveEventArgs>? MouseMove;
        public event Action<WindowGLFW, MouseWheelEventArgs>? MouseWheel;

        public event FileDropEventHandler? FileDrop;

        private bool IsDisposed => _window == null;

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
                _title = title;
                GLFW.SetWindowIcon(_window, icon.Images);

                GLFW.GetWindowSize(_window, out _clientSize.X, out _clientSize.Y);
                GLFW.GetWindowFrameSize(_window, out var left, out var top, out var right, out var bottom);
                _size = new Vector2i(_clientSize.X + left + right, _clientSize.Y + top + bottom);
                GLFW.GetWindowPos(_window, out _location.X, out _location.Y);

                GLFW.MakeContextCurrent(_window);
                GLFW.SwapInterval(1);               // Enable Vsync
                InitializeGlBindings();
                //GLFW.MakeContextCurrent(null);

                RegisterWindowCallbacks();
            }
            catch {
                GLFW.DestroyWindow(_window);
                _window = null;
                GLFW.DefaultWindowHints();
                throw;
            }
        }

        public void HandleOnce()
        {
            if(IsDisposed) { return; }

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
                // Window get disposed if closing event is handled on polling.
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

        private void RegisterWindowCallbacks()
        {
            GLFW.SetWindowPosCallback(_window, (_, x, y) =>
            {
                _location = new Vector2i(x, y);
                Move?.Invoke(this, new WindowPositionEventArgs(x, y));
            });

            GLFW.SetWindowSizeCallback(_window, (_, width, height) =>
            {
                GLFW.GetWindowFrameSize(_window, out var left, out var top, out var right, out var bottom);
                _clientSize = new Vector2i(width, height);
                _size = new Vector2i(_clientSize.X + left + right, _clientSize.Y + top + bottom);
                Resize?.Invoke(this, new ResizeEventArgs(width, height));
            });

            GLFW.SetWindowCloseCallback(_window, _ =>
            {
                var e = new CancelEventArgs();
                Closing?.Invoke(this, e);
                if(e.Cancel) {
                    GLFW.SetWindowShouldClose(_window, false);
                    return;
                }
                else {
                    Dispose();
                    return;
                }
            });

            GLFW.SetWindowIconifyCallback(_window, (_, minimized) =>
            {
                Debug.WriteLine("Iconify Not Impl");    // TODO:
                //Minimized?.Invoke(this, new MinimizedEventArgs(minimized));
            });

            GLFW.SetWindowFocusCallback(_window, (_, focused) =>
            {
                Debug.WriteLine("Focus Not Impl");    // TODO:
            });

            GLFW.SetCharCallback(_window, (_, codepoint) =>
            {
                Debug.WriteLine("Set char Not Impl");    // TODO:
            });

            GLFW.SetKeyCallback(_window, (_, key, scanCode, action, mods) =>
            {
                Debug.WriteLine("Key call Not Impl");    // TODO:
            });

            GLFW.SetCursorEnterCallback(_window, (_, entered) =>
            {
                Debug.WriteLine("Curosr Enter Not Impl");    // TODO:
            });

            GLFW.SetMouseButtonCallback(_window, (_, button, action, mods) =>
            {
                Debug.WriteLine("Mouse Button Not Impl");    // TODO:
            });

            GLFW.SetCursorPosCallback(_window, (_, x, y) =>
            {
                Debug.WriteLine("Cursor Pos Not Impl");    // TODO:
            });

            GLFW.SetScrollCallback(_window, (_, x, y) =>
            {
                Debug.WriteLine("Scroll Not Impl");    // TODO:
            });

            GLFW.SetDropCallback(_window, (_, count, paths) =>
            {
                Utf8StringRef* files;
                var isHeap = false;
                if(count > 16) {
                    isHeap = true;
                    files = (Utf8StringRef*)Marshal.AllocHGlobal(count * sizeof(Utf8StringRef));
                }
                else {
                    var p = stackalloc Utf8StringRef[count];
                    files = p;
                }
                try {
                    for(int i = 0; i < count; i++) {
                        files[i] = new Utf8StringRef(paths[i]);
                    }
                    FileDrop?.Invoke(this, new Utf8StringRefArray(files, count));
                }
                finally {
                    if(isHeap) {
                        Marshal.FreeHGlobal((IntPtr)files);
                    }
                }
            });

            GLFW.SetJoystickCallback((joystick, eventCode) =>
            {
                Debug.WriteLine("Joystick Not Impl");    // TODO:
            });

            GLFW.SetMonitorCallback((monitor, state) =>
            {
                Debug.WriteLine("Monitor Not Impl");    // TODO:
            });

            GLFW.SetWindowRefreshCallback(_window, _ =>
            {
                Debug.WriteLine("Refresh Not Impl");    // TODO:
            });
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

    internal delegate void FileDropEventHandler(WindowGLFW window, Utf8StringRefArray files);

    internal readonly ref struct WindowIconRaw
    {
        public readonly ReadOnlySpan<Image> Images;

        public static WindowIconRaw Empty => default;

        public WindowIconRaw(Span<Image> images)
        {
            Images = images;
        }
    }
}
