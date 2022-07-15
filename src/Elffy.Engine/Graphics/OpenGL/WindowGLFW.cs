#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
//using Monitor = OpenTK.Windowing.GraphicsLibraryFramework.Monitor;
using Wnd = OpenTK.Windowing.GraphicsLibraryFramework.Window;
using GLFWImage = OpenTK.Windowing.GraphicsLibraryFramework.Image;

namespace Elffy.Graphics.OpenGL
{
    /// <summary>Raw window class of GLFW</summary>
    internal unsafe sealed partial class WindowGLFW : IDisposable
    {
        const double MaxUpdateFrequency = 500;

        private readonly IHostScreen _owner;
        private Wnd* _window;
        private Vector2i _clientSize;
        private Vector2i _size;
        private Vector2i _frameBufferSize;
        private Vector2i _location;
        private string? _title;

        private double? _updateFrequency;
        private double _updateEpsilon; // 前回のアップデートからの持ちこし誤差 (sec), quantization error for Updating

        private readonly Stopwatch _watchUpdate = new Stopwatch();

        private bool _isLoaded; // It get true when the first frame is handled.

        private WindowConfig _initialConfig;
        private bool _isActivated;

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
            get => _title ?? "";
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

        public Vector2i FrameBufferSize => _frameBufferSize;

        public Vector2i Location
        {
            get => _location;
            set
            {
                if(IsDisposed) { ThrowDisposed(); }
                GLFW.SetWindowPos(_window, value.X, value.Y);
            }
        }

        public WindowGLFW(IHostScreen screen, in WindowConfig config)
        {
            _owner = screen;
            _initialConfig = config;
        }

        private void Init()
        {
            // [Hard coded setting]
            // - Opengl core profile
            // - Opengl api (not es)
            // - 4.1 or later (4.1 is the last version Mac supports)
            // Vsync is enabled

            var currentHostScreen = Engine.CurrentContext;
            var currentContext = GLFW.GetCurrentContext();

            GLFW.Init();
            GLFW.SetErrorCallback((errorCode, description) => throw new GLFWException(description, errorCode));

            var monitor = GLFW.GetPrimaryMonitor();
            if(monitor == null) {
                throw new InvalidOperationException("This computer has no monitors. GLWF cannot create window.");
            }
            GLFW.DefaultWindowHints();
            var isFullscreen = false;
            switch(_initialConfig.Style) {
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
                    throw new ArgumentException("Invalid window style");
            }

            GLFW.WindowHint(WindowHintOpenGlProfile.OpenGlProfile, OpenGlProfile.Core);
            GLFW.WindowHint(WindowHintClientApi.ClientApi, ClientApi.OpenGlApi);
            GLFW.WindowHint(WindowHintInt.ContextVersionMajor, 4);
            GLFW.WindowHint(WindowHintInt.ContextVersionMinor, 1);
            GLFW.WindowHint(WindowHintBool.OpenGLForwardCompat, true);
            GLFW.WindowHint(WindowHintBool.Focused, true);
            GLFW.WindowHint(WindowHintBool.Visible, false);
            GLFW.WindowHint(WindowHintBool.FocusOnShow, true);
            GLFW.WindowHint(WindowHintBool.Decorated, _initialConfig.BorderStyle switch
            {
                WindowBorderStyle.Default => true,
                WindowBorderStyle.NoBorder => false,
                _ => true,
            });

            var videoMode = GLFW.GetVideoMode(monitor);
            GLFW.WindowHint(WindowHintInt.RedBits, videoMode->RedBits);
            GLFW.WindowHint(WindowHintInt.GreenBits, videoMode->GreenBits);
            GLFW.WindowHint(WindowHintInt.BlueBits, videoMode->BlueBits);
            //GLFW.WindowHint(WindowHintInt.RefreshRate, videoMode->RefreshRate);
            GLFW.WindowHint(WindowHintInt.RefreshRate, (int)_initialConfig.FrameRate);
            _updateFrequency = _initialConfig.FrameRate;
            if(isFullscreen) {
                _window = GLFW.CreateWindow(videoMode->Width, videoMode->Height, _initialConfig.Title ?? "", monitor, null);
            }
            else {
                _window = GLFW.CreateWindow(_initialConfig.Width, _initialConfig.Height, _initialConfig.Title ?? "", null, null);
                GLFW.SetWindowPos(_window, (videoMode->Width - _initialConfig.Width) / 2, (videoMode->Height - _initialConfig.Height) / 2);
            }

            GLFW.GetFramebufferSize(_window, out _frameBufferSize.X, out _frameBufferSize.Y);

            try {
                MakeContextCurrent();
                InitializeGlBindings();
                RegisterWindowCallbacks();
                GLFW.FocusWindow(_window);
                _title = _initialConfig.Title;
                var icon = _initialConfig.Icon;
                if(icon.IsNone == false) {
                    using var iconImage = icon.LoadIcon();
                    var image0 = iconImage.GetImage(0);
                    fixed(ColorByte* ptr = image0) {
                        var img = new GLFWImage(image0.Width, image0.Height, (byte*)ptr);
                        GLFW.SetWindowIconRaw(_window, 1, &img);
                    }
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
                throw;
            }
            finally {
                GLFW.DefaultWindowHints();
                RestoreContextCurrent(currentContext, currentHostScreen);
            }

            if(_initialConfig.Visibility == WindowVisibility.Visible) {
                GLFW.ShowWindow(_window);
            }
        }

        public void HandleOnce()
        {
            if(!_isActivated) { ThrowNotActivated(); }
            if(IsDisposed) { return; }

            if(!_watchUpdate.IsRunning) {
                _watchUpdate.Start();
            }

            var currentHostScreen = Engine.CurrentContext;
            var currentContext = GLFW.GetCurrentContext();
            try {
                MakeContextCurrent();
                if(!_isLoaded) {
                    Debug.Assert(_isActivated);
                    _isLoaded = true;
                    Load?.Invoke(this);
                }

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
                RestoreContextCurrent(currentContext, currentHostScreen);
            }


            void Update(double elapsed)
            {
                GLFW.PollEvents();
                //if(IsDisposed) { return; }

                UpdateFrame?.Invoke(this, new FrameEventArgs(elapsed));
                //if(IsDisposed) { return; }
            }
        }

        public void SwapBuffers()
        {
            if(!_isActivated) { ThrowNotActivated(); }
            if(IsDisposed) { return; }
            GLFW.SwapBuffers(_window);
        }

        public void MakeContextCurrent()
        {
            if(!_isActivated) { ThrowNotActivated(); }
            if(IsDisposed) { ThrowDisposed(); }
            GLFW.MakeContextCurrent(_window);
            Engine.SetCurrentContext(_owner);
        }

        public void Maximize()
        {
            if(!_isActivated) { ThrowNotActivated(); }
            if(IsDisposed) { ThrowDisposed(); }
            GLFW.MaximizeWindow(_window);
        }

        public void Normalize()
        {
            if(!_isActivated) { ThrowNotActivated(); }
            if(IsDisposed) { ThrowDisposed(); }
            GLFW.RestoreWindow(_window);
        }

        public void Minimize()
        {
            if(!_isActivated) { ThrowNotActivated(); }
            if(IsDisposed) { ThrowDisposed(); }
            GLFW.IconifyWindow(_window);
        }

        public void Activate()
        {
            if(!_isActivated) {
                _isActivated = true;
                Init();
            }
            if(IsDisposed) { ThrowDisposed(); }
        }

        public void Show()
        {
            if(!_isActivated) { ThrowNotActivated(); }
            if(IsDisposed) { ThrowDisposed(); }
            GLFW.ShowWindow(_window);
        }

        public void Hide()
        {
            if(!_isActivated) { ThrowNotActivated(); }
            if(IsDisposed) { ThrowDisposed(); }
            GLFW.HideWindow(_window);
        }

        private static void RestoreContextCurrent(Wnd* context, IHostScreen? screen)
        {
            GLFW.MakeContextCurrent(context);
            Engine.SetCurrentContext(screen);
        }

        private static void InitializeGlBindings()
        {
            // This method must be called for each gl context created.

            var provider = new GLFWBindingsContext();
            try {
                GL.LoadBindings(provider);

#if false
                ReadOnlySpan<byte> name = new byte[]
                {
                    (byte)'g', (byte)'l', (byte)'S', (byte)'h', (byte)'a', (byte)'d', (byte)'e', (byte)'r',
                    (byte)'S', (byte)'o', (byte)'u', (byte)'r', (byte)'c', (byte)'e',
                };
                var glShaderSource = (delegate* unmanaged[Stdcall]<uint, int, IntPtr, int*, void>)
                    GLFW.GetProcAddressRaw((byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(name)));
#endif
            }
            catch {
                if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
#if DEBUG
                throw;
#endif
            }
        }

        public void Dispose()
        {
            if(!_isActivated || IsDisposed) { return; }  // Block re-entrant
            var window = _window;
            _window = null;
            GLFW.DestroyWindow(window);
        }

        [DoesNotReturn]
        private static void ThrowNotActivated() => throw new InvalidOperationException("The window is not activated.");

        [DoesNotReturn]
        private static void ThrowDisposed() => throw new ObjectDisposedException(nameof(WindowGLFW), "The window is already disposed.");
    }
}
