using OpenTK;
using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using Elffy.UI;
using Elffy.Core;
using Elffy.Threading;
using OpenTK.Input;
using Mouse = Elffy.Input.Mouse;
using System.Drawing;

namespace Elffy
{
    /// <summary>クロスプラットフォーム ウィンドウクラス</summary>
    public class Window : GameWindow, IScreenHost
    {
        private const string DEFAULT_WINDOW_TITLE = "Window";
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

        /// <summary>初期化時イベント</summary>
        public event EventHandler Initialized;
        /// <summary>描画前イベント</summary>
        public event EventHandler Rendering;
        /// <summary>描画後イベント</summary>
        public event EventHandler Rendered;

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
        public Window(int width, int height, string title, WindowStyle windowStyle) : base(width, height, GraphicsMode.Default, title, (GameWindowFlags)windowStyle)
        {
            VSync = VSyncMode.On;
            TargetRenderFrequency = DisplayDevice.Default.RefreshRate;
        }

        protected override void OnLoad(EventArgs e)
        {
            Dispatcher.ThrowIfNotMainThread();
            base.OnLoad(e);
            ElffySynchronizationContext.CreateIfNeeded();       // スレッドの同期コンテキスト作成
            _renderingArea.InitializeGL();
            Initialized?.Invoke(this, EventArgs.Empty);
            Layers.SystemLayer.ApplyChanging();
            foreach(var layer in Layers) {
                layer.ApplyChanging();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            Dispatcher.ThrowIfNotMainThread();
            base.OnResize(e);
            _renderingArea.Size = ClientSize;
            Camera.ChangeScreenSize(ClientSize.Width, ClientSize.Height);
        }

        protected override void OnRenderFrame(OpenTK.FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            Input.Input.Update();
            Rendering?.Invoke(this, EventArgs.Empty);
            var camera = Camera;
            _renderingArea.RenderFrame(camera.Projection, camera.View);
            Rendered?.Invoke(this, EventArgs.Empty);
            SwapBuffers();
        }

        #region Mouse State Updating
        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);
            Mouse.Update(e.Mouse);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            Mouse.Update(e.Mouse);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            Mouse.Update(e.Mouse);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            Mouse.Update(e.Mouse);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            Mouse.Update(true);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            Mouse.Update(false);
        }
        #endregion

        protected override void OnClosed(EventArgs e)
        {
            Dispatcher.ThrowIfNotMainThread();
            base.OnClosed(e);

            // 全てのレイヤーに含まれるオブジェクトを破棄し、レイヤーを削除
            _renderingArea.Layers.SystemLayer.ClearFrameObject();
            foreach(var layer in _renderingArea.Layers) {
                layer.ClearFrameObject();
            }
            _renderingArea.Layers.Clear();

            // TODO: 全オブジェクト破棄後に Dispatcher.DoInvokedAction() をする。しかしここに書くべきではない？
        }
    }
}
