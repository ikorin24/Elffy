#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;
using Elffy.Core;
using Elffy.Core.Timer;
using Elffy.InputSystem;
using Elffy.Threading;
using Elffy.UI;
using Elffy.Effective.Internal;
using FormMouseEventArgs = System.Windows.Forms.MouseEventArgs;
using OpenTK;

namespace Elffy.Platforms.Windows
{
    public class FormScreen : GLControl, IHostScreen
    {
        private bool _disposed;
        private readonly RenderingArea _renderingArea;
        private readonly SyncContextReceiver _syncContextReciever = new SyncContextReceiver();
        private IGameTimer _watch = GameTimerGenerator.Create();
        private TimeSpan _frameDelta;

        public bool IsRunning { get; private set; }

        public Mouse Mouse => _renderingArea.Mouse;

        Camera IHostScreen.Camera => _renderingArea.Camera;

        public Page UIRoot => _renderingArea.Layers.UILayer.UIRoot;

        LayerCollection IHostScreen.Layers => _renderingArea.Layers;

        public Dispatcher Dispatcher { get; } = new Dispatcher();

        public TimeSpan Time { get; private set; }

        public long FrameNum { get; private set; }

        IGameTimer IHostScreen.Watch => _watch;

        TimeSpan IHostScreen.FrameDelta => _frameDelta;

        Light IHostScreen.Light => _renderingArea.Light;

        public event ActionEventHandler<IHostScreen>? Initialized;
        public event ActionEventHandler<IHostScreen>? Rendering;
        public event ActionEventHandler<IHostScreen>? Rendered;

        public FormScreen() : this(YAxisDirection.TopToBottom) { }

        public FormScreen(YAxisDirection uiYAxisDirection)
        {
            var tmpDispatcher = Dispatcher.Current;
            Dispatcher.SwitchCurrent(Dispatcher);
            _renderingArea = new RenderingArea(Dispatcher, uiYAxisDirection);
            Dispatcher.SwitchCurrent(tmpDispatcher);
            //Engine.SwitchScreen(this);

            _frameDelta = TimeSpan.FromSeconds(1.0 / DisplayDevice.Default.RefreshRate);

            void MouseButtonDown(object sender, FormMouseEventArgs e)
            {
                var button = e.Button switch
                {
                    MouseButtons.Left => MouseButton.Left,
                    MouseButtons.Right => MouseButton.Right,
                    MouseButtons.Middle => MouseButton.Middle,
                    _ => (MouseButton?)null,
                };
                if(button == null) { return; }
                Mouse.ChangePressedState(button.Value, true);
            };

            void MouseButtonUp(object sender, FormMouseEventArgs e)
            {
                var button = e.Button switch
                {
                    MouseButtons.Left => MouseButton.Left,
                    MouseButtons.Right => MouseButton.Right,
                    MouseButtons.Middle => MouseButton.Middle,
                    _ => (MouseButton?)null,
                };
                if(button == null) { return; }
                Mouse.ChangePressedState(button.Value, false);
            };

            Resize += OnResize;
            Paint += OnPaint;
            MouseMove += (sender, e) => Mouse.ChangePosition(new Point(e.X, e.Y));
            MouseWheel += (sender, e) => Mouse.ChangeWheel(e.Delta);
            MouseDown += MouseButtonDown;
            MouseUp += MouseButtonUp;
            MouseEnter += (sender, e) => Mouse.ChangeOnScreen(true);
            MouseLeave += (sender, e) => Mouse.ChangeOnScreen(false);
        }

        public void Run()
        {
            if(IsDesignMode) { return; }
            Dispatcher.ThrowIfNotMainThread();
            IsRunning = true;
            _renderingArea.Size = ClientSize;
            Rendering += sender =>
            {
                Dispatcher.SwitchCurrent(Dispatcher);
                Engine.SwitchScreen(this);
            };
            Dispatcher.SwitchCurrent(Dispatcher);
            Engine.SwitchScreen(this);
            _renderingArea.InitializeGL();
            _watch.Start();
            Initialized?.Invoke(this);
            _renderingArea.Layers.SystemLayer.ApplyChanging();
            foreach(var layer in _renderingArea.Layers) {
                layer.ApplyChanging();
            }
            Invalidate();
        }

        void IHostScreen.Show(int width, int height, string title, Icon? icon, WindowStyle windowStyle)
        {
            var form = new Form();
            form.SuspendLayout();
            form.ClientSize = new Size(width, height);
            form.StartPosition = FormStartPosition.CenterScreen;
            form.Text = title;
            form.Icon = icon;
            switch(windowStyle) {
                case WindowStyle.Default: {
                    form.FormBorderStyle = FormBorderStyle.Sizable;
                    form.WindowState = FormWindowState.Normal;
                    break;
                }
                case WindowStyle.Fullscreen:
                    form.FormBorderStyle = FormBorderStyle.None;
                    form.WindowState = FormWindowState.Maximized;
                    form.MaximizeBox = false;
                    break;
                case WindowStyle.FixedWindow:
                    form.FormBorderStyle = FormBorderStyle.Sizable;
                    form.WindowState = FormWindowState.Normal;
                    form.MaximizeBox = false;
                    break;
                default:
                    break;
            }
            Tag = form;
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            Location = new Point(0, 0);
            Size = form.ClientSize;
            TabIndex = 0;
            form.Controls.Add(this);
            form.ResumeLayout(false);
            //form.Load += (sender, e) => Run(default(CurriedDelegateDummy).SwitchScreen);
            form.Load += (sender, e) => Run();

            if(Application.OpenForms.Count == 0) {
                Application.Run(form);
            }
            else {
                form.Show();
            }
        }

        void IHostScreen.Close()
        {
            Dispatcher.ThrowIfNotMainThread();
            if(Tag == Parent) {
                Tag = null;
                Parent.Controls.Remove(this);
            }
            Dispose();
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_SYSKEYDOWN = 0x0104;
            const int VK_F4 = 0x73;
            if(Tag == Parent && m.Msg == WM_SYSKEYDOWN && m.WParam.ToInt32() == VK_F4) {
                m.Result = IntPtr.Zero;     // Disable alt+F4 to close
            }
            else {
                base.WndProc(ref m);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if(!_disposed) {
                if(disposing) {
                    // Release managed resources here.
                    // 全てのレイヤーに含まれるオブジェクトを破棄し、レイヤーを削除
                    _renderingArea.Layers.SystemLayer.ClearFrameObject();
                    foreach(var layer in _renderingArea.Layers) {
                        layer.ClearFrameObject();
                    }
                    _renderingArea.Layers.Clear();
                    // TODO: 全オブジェクト破棄後に Dispatcher.DoInvokedAction() をする。しかしここに書くべきではない？

                    base.Dispose(disposing);
                }
                // Release unmanaged resource
                _disposed = true;
            }
        }

        private void OnResize(object sender, EventArgs e)
        {
            if(!IsRunning) { return; }
            _renderingArea.Size = ClientSize;
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            if(!IsRunning) { return; }

            Input.Update();
            Mouse.InitFrame();
            Rendering?.Invoke(this);
            _renderingArea.RenderFrame();
            _syncContextReciever.DoAll();
            _renderingArea.Layers.UILayer.HitTest(Mouse);
            Rendered?.Invoke(this);
            Time += _frameDelta;
            FrameNum++;
            SwapBuffers();
            Invalidate();
        }
    }
}
