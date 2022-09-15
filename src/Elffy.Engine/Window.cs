#nullable enable
using System;
using System.Threading;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Elffy.InputSystem;
using Elffy.Graphics.OpenGL;
using Elffy.Features.Internal;
using Elffy.Features;
using TKMouseButton = OpenTK.Windowing.GraphicsLibraryFramework.MouseButton;
using TKMouseButtonEventArgs = OpenTK.Windowing.Common.MouseButtonEventArgs;

namespace Elffy
{
    /// <summary>Cross platform window</summary>
    [DebuggerDisplay("{GetType().Name,nq} {Title}")]
    public class Window : IHostScreen
    {
        private const string DefaultTitle = "Window";
        private const int DefaultWidth = 800;
        private const int DefaultHeight = 450;

        private const float DefaultFrameRate = 60f;    // TODO: とりあえず固定で

        private LifeState _lifeState;
        private readonly WindowGLFW _windowImpl;
        private readonly RenderingArea _renderingArea;
        private TimeSpanF _frameDelta;
        private TimeSpanF _time;
        private long _frameNum;

        /// <inheritdoc/>
        public Mouse Mouse => _renderingArea.Mouse;

        /// <inheritdoc/>
        public Keyboard Keyboard => _renderingArea.Keyboard;

        /// <inheritdoc/>
        public FrameTimingPointList Timings => _renderingArea.TimingPoints;

        /// <inheritdoc/>
        RenderPipeline IHostScreen.RenderPipeline => _renderingArea.RenderPipeline;

        public LightManager Lights => _renderingArea.Lights;

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
        public TimeSpanF Time => _time;

        public TimeSpanF FrameDelta => _frameDelta;

        /// <inheritdoc/>
        public long FrameNum => _frameNum;

        /// <inheritdoc/>
        public CancellationToken RunningToken => _renderingArea.RunningToken;

        public LifeState LifeState => _lifeState;

        /// <inheritdoc/>
        public bool IsRunning => _lifeState.IsRunning() && !_renderingArea.RunningToken.IsCancellationRequested;

        /// <inheritdoc/>
        public CurrentFrameTiming CurrentTiming => _renderingArea.CurrentTiming;

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
        public Window() : this(DefaultWidth, DefaultHeight, DefaultTitle, WindowStyle.Default, ResourceFile.None) { }

        /// <summary>Create new <see cref="Window"/></summary>
        /// <param name="width">window width</param>
        /// <param name="height">window height</param>
        public Window(int width, int height) : this(width, height, DefaultTitle, WindowStyle.Default, ResourceFile.None) { }

        /// <summary>Create new <see cref="Window"/></summary>
        /// <param name="width">window width</param>
        /// <param name="height">window height</param>
        /// <param name="title">window title</param>
        public Window(int width, int height, string title) : this(width, height, title, WindowStyle.Default, ResourceFile.None) { }

        /// <summary>Create new <see cref="Window"/></summary>
        /// <param name="windowStyle">window style</param>
        public Window(WindowStyle windowStyle)
            : this(DefaultWidth, DefaultHeight, DefaultTitle, windowStyle, ResourceFile.None)
        {
        }

        /// <summary>Create new <see cref="Window"/></summary>
        /// <param name="width">window width</param>
        /// <param name="height">window height</param>
        /// <param name="title">window title</param>
        /// <param name="windowStyle">window style</param>
        public Window(int width, int height, string title, WindowStyle windowStyle)
            : this(width, height, title, windowStyle, ResourceFile.None)
        {
        }

        /// <summary>Create new <see cref="Window"/></summary>
        /// <param name="width">width of the window</param>
        /// <param name="height">height of the window</param>
        /// <param name="title">title of the window</param>
        /// <param name="windowStyle">window style</param>
        /// <param name="icon">window icon</param>
        public Window(int width, int height, string title, WindowStyle windowStyle, ResourceFile icon)
        {
            var config = new WindowConfig
            {
                Width = width,
                Height = height,
                Title = title,
                Icon = icon,
                FrameRate = DefaultFrameRate,
                Style = windowStyle,
                BorderStyle = WindowBorderStyle.Default,
                Visibility = WindowVisibility.Visible,
            };
            Ctor(out _renderingArea, out _windowImpl, config);
        }

        public Window(in WindowConfig config)
        {
            var c = config with
            {
                FrameRate = DefaultFrameRate,
            };
            Ctor(out _renderingArea, out _windowImpl, c);
        }

        private void Ctor(out RenderingArea renderingArea, out WindowGLFW windowImpl, in WindowConfig config)
        {
            if(config.Width <= 0) { throw new ArgumentOutOfRangeException(nameof(WindowConfig.Width)); }
            if(config.Height <= 0) { throw new ArgumentOutOfRangeException(nameof(WindowConfig.Height)); }

            renderingArea = new RenderingArea(this);
            windowImpl = new WindowGLFW(this, config);

            _frameDelta = TimeSpanF.FromSeconds(1.0 / config.FrameRate);
            _windowImpl.UpdateFrame += (_, e) => UpdateFrame();
            //_windowImpl.Refresh += _ => UpdateFrame();        // TODO: 複数ウィンドウの時におかしくなる
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
            _renderingArea.Disposed += () => OnClosed();

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

        public FrameAsyncEnumerable Frames(FrameTiming timing, CancellationToken cancellationToken = default)
        {
            var timingPoint = Timings.GetTiming(timing);
            return new FrameAsyncEnumerable(timingPoint, cancellationToken);
        }

        public void Hide()
        {
            if(!Engine.IsThreadMain) { ThrowNotMainThread(); }
            if(_lifeState.IsRunning() == false) { ThrowNotRunning(); }
            _windowImpl.Hide();
        }

        public void Show()
        {
            if(!Engine.IsThreadMain) { ThrowNotMainThread(); }
            if(_lifeState.IsRunning() == false) { ThrowNotRunning(); }
            _windowImpl.Show();
        }

        public void Maximize()
        {
            if(!Engine.IsThreadMain) { ThrowNotMainThread(); }
            if(_lifeState.IsRunning() == false) { ThrowNotRunning(); }
            _windowImpl.Maximize();
        }

        public void Normalize()
        {
            if(!Engine.IsThreadMain) { ThrowNotMainThread(); }
            if(_lifeState.IsRunning() == false) { ThrowNotRunning(); }
            _windowImpl.Normalize();
        }

        public void Minimize()
        {
            if(!Engine.IsThreadMain) { ThrowNotMainThread(); }
            if(_lifeState.IsRunning() == false) { ThrowNotRunning(); }
            _windowImpl.Minimize();
        }

        public void Close()
        {
            if(!Engine.IsThreadMain) { ThrowNotMainThread(); }
            if(_lifeState.IsRunning() == false) { return; }

            if(_renderingArea.RequestClose()) {
                Debug.Assert(_lifeState == LifeState.Alive);
                _lifeState = LifeState.Terminating;
            }
        }

        private void OnClosed()
        {
            Engine.RemoveScreen(this, static screen =>
            {
                var self = SafeCast.As<Window>(screen);
                self._windowImpl.Dispose();
                self._lifeState = LifeState.Dead;
            });
        }

        /// <summary>Acticate the window</summary>
        public void Activate()
        {
            if(!Engine.IsThreadMain) { ThrowNotMainThread(); }
            if(_lifeState > LifeState.New) {
                throw new InvalidOperationException("Cannot activate twice.");
            }
            _lifeState = LifeState.Activating;

            Engine.AddScreen(this, static screen =>
            {
                var self = SafeCast.As<Window>(screen);
                Debug.Assert(self._lifeState == LifeState.Activating);
                self._windowImpl.Activate();
                self._lifeState = LifeState.Alive;
            });
        }

        /// <inheritdoc/>
        void IHostScreen.HandleOnce()
        {
            if(!Engine.IsThreadMain) { ThrowNotMainThread(); }
            if(_lifeState.IsRunning() == false) { ThrowNotRunning(); }
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
        private static void ThrowNotRunning() => throw new InvalidOperationException("Window is not activated yet or already dead.");

        [DoesNotReturn]
        private static void ThrowNotMainThread() => throw new InvalidOperationException("Current thread is not main thread of the Engine.");
    }
}
