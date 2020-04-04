#nullable enable
using OpenTK;
using OpenTK.Graphics;
using System;
using Elffy.UI;
using Elffy.Core;
using Elffy.Threading;
using Elffy.InputSystem;
using System.Drawing;
using TKMouseButton = OpenTK.Input.MouseButton;
using TKMouseButtonEventArgs = OpenTK.Input.MouseButtonEventArgs;
using Elffy.Core.Timer;
using Elffy.Effective.Internal;

namespace Elffy
{
    /// <summary>クロスプラットフォーム ウィンドウクラス</summary>
    public class Window : IHostScreen
    {
        private bool _isClosed;
        private const string DEFAULT_WINDOW_TITLE = "Window";
        private readonly GameWindow _window;
        /// <summary>描画領域に関する処理を行うオブジェクト</summary>
        private readonly RenderingArea _renderingArea;
        private readonly SyncContextReceiver _syncContextReciever = new SyncContextReceiver();
        private readonly IGameTimer _watch = GameTimerGenerator.Create();
        private TimeSpan _frameDelta;

        /// <summary>ウィンドウの UI の Root</summary>
        public Page UIRoot => _renderingArea.Layers.UILayer.UIRoot;

        /// <summary>マウスを取得します</summary>
        public Mouse Mouse => _renderingArea.Mouse;

        /// <summary>このウィンドウのレイヤー</summary>
        LayerCollection IHostScreen.Layers => _renderingArea.Layers;
        /// <summary>カメラを取得します</summary>
        Camera IHostScreen.Camera => _renderingArea.Camera;
        TimeSpan IHostScreen.FrameDelta => _frameDelta;
        Light IHostScreen.Light => _renderingArea.Light;
        IGameTimer IHostScreen.Watch => _watch;

        public VSyncMode VSync { get => _window.VSync; set => _window.VSync = value; }

        public Size ClientSize { get => _window.ClientSize; set => _window.ClientSize = value; }

        public Icon Icon { get => _window.Icon; set => _window.Icon = value; }

        public string Title { get => _window.Title; set => _window.Title = value; }

        public Dispatcher Dispatcher => _renderingArea.Dispatcher;

        public TimeSpan Time { get; private set; }

        public long FrameNum { get; private set; }

        /// <summary>初期化時イベント</summary>
        public event ActionEventHandler<IHostScreen>? Initialized;
        /// <summary>描画前イベント</summary>
        public event ActionEventHandler<IHostScreen>? Rendering;
        /// <summary>描画後イベント</summary>
        public event ActionEventHandler<IHostScreen>? Rendered;

        /// <summary>ウィンドウを作成します</summary>
        public Window() : this(WindowStyle.Default) { }

        public Window(YAxisDirection uiYAxisDirection) : this(800, 450, DEFAULT_WINDOW_TITLE, WindowStyle.Default, uiYAxisDirection) { }

        /// <summary>スタイルを指定してウィンドウを作成します</summary>
        /// <param name="windowStyle">ウィンドウのスタイル</param>
        public Window(WindowStyle windowStyle) : this(800, 450, DEFAULT_WINDOW_TITLE, windowStyle, YAxisDirection.TopToBottom) { }

        /// <summary>サイズとタイトルとスタイルを指定して、ウィンドウを作成します</summary>
        /// <param name="width">ウィンドウの幅</param>
        /// <param name="height">ウィンドウの高さ</param>
        /// <param name="title">ウィンドウのタイトル</param>
        /// <param name="windowStyle">ウィンドウのスタイル</param>
        /// <param name="yAxisDirection">Y軸の方向</param>
        public Window(int width, int height, string title, WindowStyle windowStyle, YAxisDirection yAxisDirection)
        {
            _renderingArea = new RenderingArea(yAxisDirection, this);
            _window = new GameWindow(width, height, GraphicsMode.Default, title, (GameWindowFlags)windowStyle);
            if(windowStyle != WindowStyle.Fullscreen) {
                _window.ClientSize = new Size(width, height);
            }
            _window.VSync = VSyncMode.On;
            _window.TargetRenderFrequency = DisplayDevice.Default.RefreshRate;
            _frameDelta = TimeSpan.FromSeconds(1.0 / _window.TargetRenderFrequency);
            _window.Load += OnLoad;
            _window.Resize += OnResize;
            _window.RenderFrame += OnRenderFrame;

            MouseButton? GetMouseButton(TKMouseButton button) => button switch
            {
                TKMouseButton.Left => MouseButton.Left,
                TKMouseButton.Right => MouseButton.Right,
                TKMouseButton.Middle => MouseButton.Middle,
                _ => (MouseButton?)null
            };

            void MouseButtonStateChanged(object sender, TKMouseButtonEventArgs e)
            {
                var button = GetMouseButton(e.Button);
                if(button == null) { return; }
                Mouse.ChangePressedState(button.Value, e.IsPressed);
            };

            _window.MouseMove += (sender, e) => Mouse.ChangePosition(new Point(e.X, e.Y));
            _window.MouseWheel += (sender, e) => Mouse.ChangeWheel(e.Mouse.WheelPrecise);
            _window.MouseDown += MouseButtonStateChanged;
            _window.MouseUp += MouseButtonStateChanged;
            _window.MouseEnter += (sender, e) => Mouse.ChangeOnScreen(true);
            _window.MouseLeave += (sender, e) => Mouse.ChangeOnScreen(false);
        }

        void IHostScreen.Close()
        {
            Dispatcher.ThrowIfNotMainThread();
            if(_isClosed) { return; }
            _isClosed = true;
            _window.Close();
            ReleaseResource();
        }

        void IHostScreen.Show(int width, int height, string title, Icon? icon, WindowStyle windowStyle)
        {
            Title = title;
            if(icon != null) { Icon = icon; }
            Rendering += default(CurriedDelegateDummy).SwitchScreen;
            switch(windowStyle) {
                case WindowStyle.Default: {
                    _window.WindowBorder = WindowBorder.Resizable;
                    _window.WindowState = WindowState.Normal;
                    break;
                }
                case WindowStyle.Fullscreen: {
                    _window.WindowBorder = WindowBorder.Fixed;
                    _window.WindowState = WindowState.Fullscreen;
                    break;
                }
                case WindowStyle.FixedWindow: {
                    _window.WindowBorder = WindowBorder.Fixed;
                    _window.WindowState = WindowState.Normal;
                    break;
                }
                default:
                    break;
            }
            _window.Run();
        }

        private void OnLoad(object sender, EventArgs e)
        {
            Dispatcher.ThrowIfNotMainThread();
            CustomSynchronizationContext.CreateIfNeeded(_syncContextReciever);
            _renderingArea.InitializeGL();
            _watch.Start();
            Engine.SwitchScreen(default, this);
            Initialized?.Invoke(this);
            _renderingArea.Layers.SystemLayer.ApplyChanging();
            foreach(var layer in _renderingArea.Layers) {
                layer.ApplyChanging();
            }
        }

        private void OnResize(object sender, EventArgs e)
        {
            Dispatcher.ThrowIfNotMainThread();
            _renderingArea.Size = _window.ClientSize;
        }

        private void OnRenderFrame(object sender, FrameEventArgs e)
        {
            Input.Update();
            Mouse.InitFrame();
            Rendering?.Invoke(this);
            _renderingArea.RenderFrame();
            _syncContextReciever.DoAll();
            _renderingArea.Layers.UILayer.HitTest(Mouse);
            Rendered?.Invoke(this);
            Time += _frameDelta;
            FrameNum++;
            _window.SwapBuffers();
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
    }
}
