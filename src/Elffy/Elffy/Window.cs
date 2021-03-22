#nullable enable
using System;
using System.Threading;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Elffy.UI;
using Elffy.Core;
using Elffy.InputSystem;
using Elffy.OpenGL;
using Elffy.Imaging;
using TKMouseButton = OpenTK.Windowing.GraphicsLibraryFramework.MouseButton;
using TKMouseButtonEventArgs = OpenTK.Windowing.Common.MouseButtonEventArgs;

namespace Elffy
{
    /// <summary>Cross platform window</summary>
    [DebuggerDisplay("Window ({Title})")]
    public class Window : IHostScreen
    {
        private const string DefaultTitle = "Window";

        private bool _isActivated;
        private readonly WindowGLFW _windowImpl;
        private readonly RenderingArea _renderingArea;
        private TimeSpan _frameDelta;
        private TimeSpan _time;
        private long _frameNum;

        /// <inheritdoc/>
        public RootPanel UIRoot => _renderingArea.Layers.UILayer.UIRoot;

        /// <inheritdoc/>
        public Mouse Mouse => _renderingArea.Mouse;
        
        /// <inheritdoc/>
        public Keyboard Keyboard => _renderingArea.Keyboard;

        /// <inheritdoc/>
        public AsyncBackEndPoint AsyncBack => _renderingArea.AsyncBack;

        /// <inheritdoc/>
        public LayerCollection Layers => _renderingArea.Layers;

        /// <inheritdoc/>
        public Camera Camera => _renderingArea.Camera;

        /// <inheritdoc/>
        public Vector2i ClientSize { get => _windowImpl.ClientSize; set => _windowImpl.ClientSize = value; }

        /// <inheritdoc/>
        public Vector2i FrameBufferSize => _windowImpl.FrameBufferSize;

        /// <inheritdoc/>
        public Vector2i Location { get => _windowImpl.Location; set => _windowImpl.Location = value; }

        /// <inheritdoc/>
        public string Title { get => _windowImpl.Title; set => _windowImpl.Title = value; }

        /// <inheritdoc/>
        public TimeSpan Time => _time;

        public TimeSpan FrameDelta => _frameDelta;

        /// <inheritdoc/>
        public long FrameNum => _frameNum;

        public FrameEnumerableSource Frames => _renderingArea.Frames;

        /// <inheritdoc/>
        public CancellationToken RunningToken => _renderingArea.RunningToken;

        /// <inheritdoc/>
        public bool IsRunning => _windowImpl.IsRunning && !_renderingArea.RunningToken.IsCancellationRequested;

        /// <inheritdoc/>
        public ScreenCurrentTiming CurrentTiming => _renderingArea.CurrentTiming;

        /// <inheritdoc/>
        public event Action<IHostScreen>? Initialized
        {
            add => _renderingArea.Initialized += value;
            remove => _renderingArea.Initialized -= value;
        }

        /// <inheritdoc/>
        public event ClosingEventHandler<IHostScreen>? Closing
        {
            add => _renderingArea.Closing += value;
            remove => _renderingArea.Closing -= value;
        }

        /// <summary>Create new <see cref="Window"/></summary>
        public Window() : this(WindowStyle.Default) { }

        /// <summary>Create new <see cref="Window"/></summary>
        /// <param name="windowStyle">window style</param>
        public Window(WindowStyle windowStyle)
        {
            var icon = Icon.Empty;
            Ctor(out _renderingArea, out _windowImpl, 800, 450, DefaultTitle, windowStyle, ref icon);
        }

        /// <summary>Create new <see cref="Window"/></summary>
        /// <param name="width">width of the window</param>
        /// <param name="height">height of the window</param>
        /// <param name="title">title of the window</param>
        /// <param name="windowStyle">window style</param>
        /// <param name="icon">window icon</param>
        public Window(int width, int height, string title, WindowStyle windowStyle, ref Icon icon)
        {
            Ctor(out _renderingArea, out _windowImpl, width, height, title, windowStyle, ref icon);
        }

        private void Ctor(out RenderingArea renderingArea, out WindowGLFW windowImpl, int width, int height, string title, WindowStyle windowStyle, ref Icon icon)
        {
            renderingArea = new RenderingArea(this);
            windowImpl = new WindowGLFW(this, width, height, title, windowStyle, ref icon);

            _frameDelta = TimeSpan.FromSeconds(1.0 / 60.0); // TODO: とりあえず固定で
            _windowImpl.UpdateFrame += (_, e) => UpdateFrame();
            _windowImpl.Refresh += _ => UpdateFrame();
            _windowImpl.Load += _ => _renderingArea.Initialize();
            _windowImpl.FrameBufferSizeChanged += (_, size) => _renderingArea.SetFrameBufferSize(size);
            _windowImpl.MouseMove += (_, e) => Mouse.ChangePosition(e.Position);
            _windowImpl.MouseWheel += (_, e) => Mouse.ChangeWheel(e.OffsetY);
            _windowImpl.MouseDown += MouseButtonStateChanged;
            _windowImpl.MouseUp += MouseButtonStateChanged;
            _windowImpl.MouseEnter += _ => Mouse.ChangeOnScreen(true);
            _windowImpl.MouseLeave += _ => Mouse.ChangeOnScreen(false);
            _windowImpl.KeyDown += (_, e) => Keyboard.ChangeToDown(e);
            _windowImpl.KeyUp += (_, e) => Keyboard.ChangeToUp(e);
            _windowImpl.Closing += (_, e) => Close();

            _renderingArea.Disposed += () =>
            {
                _windowImpl.Dispose();
                Engine.RemoveScreen(this);
            };

            void MouseButtonStateChanged(WindowGLFW _, TKMouseButtonEventArgs e)
            {
                switch(e.Button) {
                    case TKMouseButton.Left:
                        Mouse.ChangePressedState(MouseButton.Left, e.IsPressed);
                        break;
                    case TKMouseButton.Middle:
                        Mouse.ChangePressedState(MouseButton.Middle, e.IsPressed);
                        break;
                    case TKMouseButton.Right:
                        Mouse.ChangePressedState(MouseButton.Right, e.IsPressed);
                        break;
                    default:
                        return;
                }
            };
        }

        public void Maximize()
        {
            if(!Engine.IsThreadMain) { ThrowNotMainThread(); }
            if(!_isActivated) { ThrowNotActivated(); }
            _windowImpl.Maximize();
        }

        public void Normalize()
        {
            if(!Engine.IsThreadMain) { ThrowNotMainThread(); }
            if(!_isActivated) { ThrowNotActivated(); }
            _windowImpl.Normalize();
        }

        public void Minimize()
        {
            if(!Engine.IsThreadMain) { ThrowNotMainThread(); }
            if(!_isActivated) { ThrowNotActivated(); }
            _windowImpl.Minimize();
        }

        public void Close()
        {
            if(!Engine.IsThreadMain) { ThrowNotMainThread(); }
            if(!_isActivated) { return; }
            _renderingArea.RequestClose();
        }

        /// <summary>Acticate the window</summary>
        public void Activate()
        {
            if(!Engine.IsThreadMain) { ThrowNotMainThread(); }
            if(_isActivated == false) {
                _isActivated = true;
                _windowImpl.Activate();
                Engine.AddScreen(this);
            }
        }

        /// <inheritdoc/>
        void IHostScreen.HandleOnce()
        {
            if(!Engine.IsThreadMain) { ThrowNotMainThread(); }
            if(!_isActivated) { ThrowNotActivated(); }
            _windowImpl.HandleOnce();
        }

        private void UpdateFrame()
        {
            _renderingArea.RenderFrame();
            _time += _frameDelta;
            _frameNum++;
            _windowImpl.SwapBuffers();
        }

        [DoesNotReturn]
        private static void ThrowNotActivated() => throw new InvalidOperationException("Window is not activated yet.");

        [DoesNotReturn]
        private static void ThrowNotMainThread() => throw new InvalidOperationException("Current thread is not main thread of the Engine.");
    }
}
