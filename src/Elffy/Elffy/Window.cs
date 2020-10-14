#nullable enable
using System;
using Elffy.UI;
using Elffy.Core;
using Elffy.InputSystem;
using OpenTK.Windowing.Common;
using TKMouseButton = OpenTK.Windowing.GraphicsLibraryFramework.MouseButton;
using TKMouseButtonEventArgs = OpenTK.Windowing.Common.MouseButtonEventArgs;
using Elffy.OpenGL;
using Elffy.OpenGL.Windowing;
using Elffy.Threading.Tasks;

namespace Elffy
{
    /// <summary>クロスプラットフォーム ウィンドウクラス</summary>
    public class Window : IHostScreen
    {
        private const string DefaultTitle = "Window";

        private bool _isClosed;
        private readonly WindowGLFW _windowImpl;

        [ThreadStatic]
        private static bool _isThreadMain;

        /// <summary>描画領域に関する処理を行うオブジェクト</summary>
        private readonly RenderingArea _renderingArea;
        private TimeSpan _frameDelta;
        private TimeSpan _time;
        private long _frameNum;
        private readonly DefaultGLResource _defaultGLResource;

        /// <summary>ウィンドウの UI の Root</summary>
        public RootPanel UIRoot => _renderingArea.Layers.UILayer.UIRoot;

        /// <summary>マウスを取得します</summary>
        public Mouse Mouse => _renderingArea.Mouse;

        public AsyncBackEndPoint AsyncBack => _renderingArea.AsyncBack;

        /// <summary>このウィンドウのレイヤー</summary>
        LayerCollection IHostScreen.Layers => _renderingArea.Layers;
        /// <summary>カメラを取得します</summary>
        Camera IHostScreen.Camera => _renderingArea.Camera;

        public bool IsEnabledPostProcess
        {
            get => _renderingArea.IsEnabledPostProcess;
            set => _renderingArea.IsEnabledPostProcess = value;
        }

        TimeSpan IHostScreen.FrameDelta => _frameDelta;

        public Vector2i ClientSize { get => _windowImpl.ClientSize; set => _windowImpl.ClientSize = value; }

        public Vector2i Location { get => _windowImpl.Location; set => _windowImpl.Location = value; }

        public string Title { get => _windowImpl.Title; set => _windowImpl.Title = value; }

        /// <inheritdoc/>
        public ref readonly TimeSpan Time => ref _time;

        /// <inheritdoc/>
        public ref readonly long FrameNum => ref _frameNum;

        public IDefaultResource DefaultResource => _defaultGLResource;

        /// <summary>初期化時イベント</summary>
        public event ActionEventHandler<IHostScreen>? Initialized;

        /// <summary>ウィンドウを作成します</summary>
        public Window() : this(WindowStyle.Default) { }

        /// <summary>スタイルを指定してウィンドウを作成します</summary>
        /// <param name="windowStyle">ウィンドウのスタイル</param>
        public Window(WindowStyle windowStyle) : this(800, 450, DefaultTitle, windowStyle) { }

        /// <summary>サイズとタイトルとスタイルを指定して、ウィンドウを作成します</summary>
        /// <param name="width">ウィンドウの幅</param>
        /// <param name="height">ウィンドウの高さ</param>
        /// <param name="title">ウィンドウのタイトル</param>
        /// <param name="windowStyle">ウィンドウのスタイル</param>
        public Window(int width, int height, string title, WindowStyle windowStyle)
        {
            _isThreadMain = true;
            _renderingArea = new RenderingArea(this);

            _windowImpl = new WindowGLFW(width, height, title, windowStyle, false, WindowIconRaw.Empty);  // TODO: アンチエイリアス有効化した時のポストプロセスのバッファサイズが未対応

            _frameDelta = TimeSpan.FromSeconds(1.0 / 60.0); // TODO: とりあえず固定で
            _windowImpl.UpdateFrame += OnUpdateFrame;
            _windowImpl.Load += OnLoad;
            _windowImpl.Closed += _ => Dispose();
            _windowImpl.Resize += OnResize;
            _windowImpl.MouseMove += (_, e) => Mouse.ChangePosition(e.Position);
            _windowImpl.MouseWheel += (_, e) => Mouse.ChangeWheel(e.OffsetY);
            _windowImpl.MouseDown += MouseButtonStateChanged;
            _windowImpl.MouseUp += MouseButtonStateChanged;
            _windowImpl.MouseEnter += _ => Mouse.ChangeOnScreen(true);
            _windowImpl.MouseLeave += _ => Mouse.ChangeOnScreen(false);

            _defaultGLResource = new DefaultGLResource();
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

        public void Dispose()
        {
            ThrowIfNotMainThread();
            if(_isClosed) { return; }
            _isClosed = true;
            _windowImpl.Dispose();
            _renderingArea.Dispose();
            _defaultGLResource.Dispose();
            Engine.RemoveScreen(this);
        }

        /// <inheritdoc/>
        public bool IsThreadMain()
        {
            return _isThreadMain;
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
            _renderingArea.InitializeGL();
            _defaultGLResource.Init();
            Initialized?.Invoke(this);

            var layers = _renderingArea.Layers;
            layers.SystemLayer.ApplyAdd();
            foreach(var layer in layers.AsReadOnlySpan()) {
                layer.ApplyAdd();
            }
            layers.UILayer.ApplyAdd();
        }

        private void OnResize(WindowGLFW _, ResizeEventArgs e)
        {
            _renderingArea.Size = e.Size;
        }

        private void OnUpdateFrame(WindowGLFW _, FrameEventArgs e)
        {
            Mouse.InitFrame();
            _renderingArea.RenderFrame();
            _renderingArea.Layers.UILayer.HitTest(Mouse);
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
