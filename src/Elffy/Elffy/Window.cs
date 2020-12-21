#nullable enable
using System;
using System.Threading;
using System.ComponentModel;
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
    public class Window : IHostScreen
    {
        private const string DefaultTitle = "Window";

        [ThreadStatic]
        private static bool _isThreadMain;

        private readonly WindowGLFW _windowImpl;
        private readonly RenderingArea _renderingArea;
        private TimeSpan _frameDelta;
        private TimeSpan _time;
        private long _frameNum;

        /// <summary>Get UI root of the window</summary>
        public RootPanel UIRoot => _renderingArea.Layers.UILayer.UIRoot;

        /// <inheritdoc/>
        public Mouse Mouse => _renderingArea.Mouse;
        
        /// <inheritdoc/>
        public Keyboard Keyboard => _renderingArea.Keyboard;

        public AsyncBackEndPoint AsyncBack => _renderingArea.AsyncBack;

        public LayerCollection Layers => _renderingArea.Layers;

        public Camera Camera => _renderingArea.Camera;

        public Vector2i ClientSize { get => _windowImpl.ClientSize; set => _windowImpl.ClientSize = value; }

        public Vector2i Location { get => _windowImpl.Location; set => _windowImpl.Location = value; }

        public string Title { get => _windowImpl.Title; set => _windowImpl.Title = value; }

        /// <inheritdoc/>
        public ref readonly TimeSpan Time => ref _time;

        /// <inheritdoc/>
        public ref readonly long FrameNum => ref _frameNum;

        public IDefaultResource DefaultResource => _renderingArea.DefaultGLResource;

        /// <inheritdoc/>
        public CancellationToken RunningToken => _renderingArea.RunningToken;

        /// <inheritdoc/>
        public ScreenCurrentTiming CurrentTiming => _renderingArea.CurrentTiming;

        /// <inheritdoc/>
        public bool IsThreadMain => _isThreadMain;

        /// <inheritdoc/>
        public event ActionEventHandler<IHostScreen>? Initialized
        {
            add => _renderingArea.Initialized += value;
            remove => _renderingArea.Initialized -= value;
        }

        /// <inheritdoc/>
        public event Action<IHostScreen, CancelEventArgs>? Closing
        {
            add => _renderingArea.Closing += value;
            remove => _renderingArea.Closing -= value;
        }

        /// <summary>Create new <see cref="Window"/></summary>
        public Window() : this(WindowStyle.Default) { }

        /// <summary>Create new <see cref="Window"/></summary>
        /// <param name="windowStyle">window style</param>
        public Window(WindowStyle windowStyle) : this(800, 450, DefaultTitle, windowStyle, ReadOnlySpan<RawImage>.Empty) { }

        /// <summary>Create new <see cref="Window"/></summary>
        /// <param name="width">width of the window</param>
        /// <param name="height">height of the window</param>
        /// <param name="title">title of the window</param>
        /// <param name="windowStyle">window style</param>
        public Window(int width, int height, string title, WindowStyle windowStyle, ReadOnlySpan<RawImage> icon)
        {
            _isThreadMain = true;
            _renderingArea = new RenderingArea(this);
            _windowImpl = new WindowGLFW(width, height, title, windowStyle, icon);

            _frameDelta = TimeSpan.FromSeconds(1.0 / 60.0); // TODO: とりあえず固定で
            _windowImpl.UpdateFrame += (_, e) => UpdateFrame();
            _windowImpl.Refresh += _ => UpdateFrame();
            _windowImpl.Load += OnLoad;
            _windowImpl.Resize += OnResize;
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

        public void Close()
        {
            ThrowIfNotMainThread();
            _renderingArea.RequestClose();
        }

        /// <inheritdoc/>
        public void ThrowIfNotMainThread()
        {
            if(!_isThreadMain) {
                ThrowThreadNotMain();
                static void ThrowThreadNotMain() => throw new InvalidOperationException("Current thread is not main thread.");
            }
        }

        private void OnLoad(WindowGLFW _)
        {
            ThrowIfNotMainThread();
            _renderingArea.Initialize();
        }

        private void OnResize(WindowGLFW _, ResizeEventArgs e)
        {
            _renderingArea.Size = e.Size;
        }

        private void UpdateFrame()
        {
            _renderingArea.RenderFrame();
            _time += _frameDelta;
            _frameNum++;
            _windowImpl.SwapBuffers();
        }

        public void Show()
        {
            _windowImpl.Show();
        }

        void IHostScreen.HandleOnce()
        {
            _windowImpl.HandleOnce();
        }
    }
}
