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
using OpenTK.Windowing.Common;
using TKMouseButton = OpenTK.Windowing.GraphicsLibraryFramework.MouseButton;
using TKMouseButtonEventArgs = OpenTK.Windowing.Common.MouseButtonEventArgs;

namespace Elffy
{
    /// <summary>Cross platform window</summary>
    [DebuggerDisplay("Window ({Title})")]
    public class Window : IHostScreen
    {
        private const string DefaultTitle = "Window";

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
        public Vector2 ContentScale => _windowImpl.ContentScale;

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
        public Window(WindowStyle windowStyle) : this(800, 450, DefaultTitle, windowStyle, Icon.Empty) { }

        /// <summary>Create new <see cref="Window"/></summary>
        /// <param name="width">width of the window</param>
        /// <param name="height">height of the window</param>
        /// <param name="title">title of the window</param>
        /// <param name="windowStyle">window style</param>
        /// <param name="icon">window icon</param>
        public Window(int width, int height, string title, WindowStyle windowStyle, Icon icon)
        {
            _renderingArea = new RenderingArea(this);
            _windowImpl = new WindowGLFW(this, width, height, title, windowStyle, icon);

            _frameDelta = TimeSpan.FromSeconds(1.0 / 60.0); // TODO: とりあえず固定で
            _windowImpl.UpdateFrame += (_, e) => UpdateFrame();
            _windowImpl.Refresh += _ => UpdateFrame();
            _windowImpl.Load += OnLoad;
            _windowImpl.Resize += (_, e) => _renderingArea.SetClientSize(e.Size);
            _windowImpl.ContentScaleChanged += (_, scale) => _renderingArea.SetContentScale(scale);
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

            Engine.AddScreen(this, show: false);


            void MouseButtonStateChanged(WindowGLFW _, TKMouseButtonEventArgs e)
            {
                MouseButton button;
                switch(e.Button) {
                    case TKMouseButton.Left:
                        button = MouseButton.Left;
                        break;
                    case TKMouseButton.Middle:
                        button = MouseButton.Middle;
                        break;
                    case TKMouseButton.Right:
                        button = MouseButton.Right;
                        break;
                    default:
                        return;
                }
                Mouse.ChangePressedState(button, e.IsPressed);
            };
        }

        public void Maximize()
        {
            if(!Engine.IsThreadMain) { ThrowNotMainThread(); }
            _windowImpl.Maximize();
        }

        public void Normalize()
        {
            if(!Engine.IsThreadMain) { ThrowNotMainThread(); }
            _windowImpl.Normalize();
        }

        public void Minimize()
        {
            if(!Engine.IsThreadMain) { ThrowNotMainThread(); }
            _windowImpl.Minimize();
        }

        public void Close()
        {
            if(!Engine.IsThreadMain) { ThrowNotMainThread(); }
            _renderingArea.RequestClose();
        }

        /// <summary>Show the window</summary>
        public void Show()
        {
            if(!Engine.IsThreadMain) { ThrowNotMainThread(); }
            _windowImpl.Show();
        }

        /// <inheritdoc/>
        void IHostScreen.HandleOnce()
        {
            if(!Engine.IsThreadMain) { ThrowNotMainThread(); }
            _windowImpl.HandleOnce();
        }

        private void OnLoad(WindowGLFW _)
        {
            if(!Engine.IsThreadMain) { ThrowNotMainThread(); }
            _renderingArea.Initialize();
        }

        private void UpdateFrame()
        {
            _renderingArea.RenderFrame();
            _time += _frameDelta;
            _frameNum++;
            _windowImpl.SwapBuffers();
        }

        [DoesNotReturn]
        private static void ThrowNotMainThread() => throw new InvalidOperationException("Current thread is not main thread of the Engine.");
    }
}
