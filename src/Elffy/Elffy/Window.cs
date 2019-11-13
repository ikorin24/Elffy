#nullable enable
using OpenTK;
using OpenTK.Graphics;
using System;
using Elffy.UI;
using Elffy.Core;
using Elffy.Threading;
using Elffy.Input;
using System.Drawing;

namespace Elffy
{
    /// <summary>クロスプラットフォーム ウィンドウクラス</summary>
    public class Window : IScreenHost
    {
        private bool _isClosed;
        private const string DEFAULT_WINDOW_TITLE = "Window";
        private readonly GameWindow _window;
        /// <summary>描画領域に関する処理を行うオブジェクト</summary>
        private readonly RenderingArea _renderingArea = new RenderingArea();

        /// <summary>ウィンドウの UI の Root</summary>
        public IUIRoot UIRoot => _renderingArea.UIRoot;
        /// <summary>このウィンドウのレイヤー</summary>
        public LayerCollection Layers => _renderingArea.Layers;

        /// <summary>マウスを取得します</summary>
        public Mouse Mouse { get; } = new Mouse();

        /// <summary>カメラを取得します</summary>
        public Camera Camera { get; } = new Camera();
        public VSyncMode VSync { get => _window.VSync; set => _window.VSync = value; }

        public double TargetRenderPeriod { get => _window.TargetRenderPeriod; set => _window.TargetRenderPeriod = value; }

        public Size ClientSize { get => _window.ClientSize; set => _window.ClientSize = value; }

        public Icon Icon { get => _window.Icon; set => _window.Icon = value; }

        public string Title { get => _window.Title; set => _window.Title = value; }

        /// <summary>初期化時イベント</summary>
        public event ActionEventHandler<IScreenHost>? Initialized;
        /// <summary>描画前イベント</summary>
        public event ActionEventHandler<IScreenHost>? Rendering;
        /// <summary>描画後イベント</summary>
        public event ActionEventHandler<IScreenHost>? Rendered;

        /// <summary>ウィンドウを作成します</summary>
        public Window() : this(800, 450, DEFAULT_WINDOW_TITLE, WindowStyle.Default) { }

        /// <summary>スタイルを指定してウィンドウを作成します</summary>
        /// <param name="windowStyle">ウィンドウのスタイル</param>
        public Window(WindowStyle windowStyle) : this(800, 450, DEFAULT_WINDOW_TITLE, windowStyle) { }

        /// <summary>サイズとタイトルとスタイルを指定して、ウィンドウを作成します</summary>
        /// <param name="width">ウィンドウの幅</param>
        /// <param name="height">ウィンドウの高さ</param>
        /// <param name="title">ウィンドウのタイトル</param>
        /// <param name="windowStyle">ウィンドウのスタイル</param>
        public Window(int width, int height, string title, WindowStyle windowStyle)
        {
            _window = new GameWindow(width, height, GraphicsMode.Default, title, (GameWindowFlags)windowStyle);
            _window.VSync = VSyncMode.On;
            _window.TargetRenderFrequency = DisplayDevice.Default.RefreshRate;

            // event
            _window.Load += (sender, e) => OnLoad(EventArgs.Empty);
            _window.Resize += (sender, e) => OnResize(EventArgs.Empty);
            _window.RenderFrame += (sender, e) => OnRenderFrame(null);
            _window.MouseMove += (sender, e) => Mouse.Update(e.Mouse);
            _window.MouseWheel += (sender, e) => Mouse.Update(e.Mouse);
            _window.MouseDown += (sender, e) => Mouse.Update(e.Mouse);
            _window.MouseUp += (sender, e) => Mouse.Update(e.Mouse);
            _window.MouseEnter += (sender, e) => Mouse.Update(true);
            _window.MouseLeave += (sender, e) => Mouse.Update(false);
        }

        protected virtual void OnLoad(EventArgs e)
        {
            Dispatcher.ThrowIfNotMainThread();
            ElffySynchronizationContext.CreateIfNeeded();
            _renderingArea.InitializeGL();
            Initialized?.Invoke(this);
            Layers.SystemLayer.ApplyChanging();
            foreach(var layer in Layers) {
                layer.ApplyChanging();
            }
        }

        protected virtual void OnResize(EventArgs e)
        {
            Dispatcher.ThrowIfNotMainThread();
            _renderingArea.Size = _window.ClientSize;
            Camera.ChangeScreenSize(_window.ClientSize.Width, _window.ClientSize.Height);
        }

        protected virtual void OnRenderFrame(FrameEventArgs? e)
        {
            Input.Input.Update();
            Rendering?.Invoke(this);
            var camera = Camera;
            _renderingArea.RenderFrame(camera.Projection, camera.View);
            Rendered?.Invoke(this);
            _window.SwapBuffers();
        }

        public void Close()
        {
            Dispatcher.ThrowIfNotMainThread();
            if(_isClosed) { return; }
            _isClosed = true;
            _window.Close();
            ReleaseResource();
        }

        public void Show()
        {
            ThrowIfClosed();
            _window.Run();
        }

        private void ReleaseResource()
        {
            // 全てのレイヤーに含まれるオブジェクトを破棄し、レイヤーを削除
            _renderingArea.Layers.SystemLayer.ClearFrameObject();
            foreach(var layer in _renderingArea.Layers) {
                layer.ClearFrameObject();
            }
            _renderingArea.Layers.Clear();

            // TODO: 全オブジェクト破棄後に Dispatcher.DoInvokedAction() をする。しかしここに書くべきではない？
        }

        private void ThrowIfClosed()
        {
            if(_isClosed) { throw new InvalidOperationException("Window is already closed."); }
        }
    }
}
