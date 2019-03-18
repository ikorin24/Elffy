using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Elffy.Input;
using Elffy.UI;
using Elffy.Core;
using System.Drawing;

namespace Elffy
{
    public class ElffyWindow : IDisposable
    {
        private static MainWindowImplement _mainWindow;

        public event EventHandler Initialize;
        public event FrameEventHandler FrameRendering;
        public event EventHandler Closed;

        public Size ClientSize => _mainWindow.ClientSize;

        public ElffyWindow(int width, int heigh, string title, WindowStyle windowStyle) 
            => _mainWindow = new MainWindowImplement(width, heigh, title, windowStyle, this);

        public void Run() => _mainWindow.Run();

        public void Dispose() => _mainWindow.Dispose();

        #region MainWindowImplement
        private class MainWindowImplement : GameWindow, IDisposable
        {
            private ElffyWindow _elffyWindow;

            public MainWindowImplement(int width, int heigh, string title, WindowStyle windowStyle, ElffyWindow elffyWindow)
                : base(width, heigh, GraphicsMode.Default, title, (GameWindowFlags)windowStyle)
            {
                _elffyWindow = elffyWindow;
            }

            protected override void OnLoad(EventArgs e)
            {
                base.OnLoad(e);
                // OpenGLの初期設定
                GL.ClearColor(Color4.Black);
                GL.Enable(EnableCap.DepthTest);
                GL.Enable(EnableCap.Texture2D);
                VSync = VSyncMode.On;

                _elffyWindow.Initialize?.Invoke(this, e);
            }

            protected override void OnRenderFrame(OpenTK.FrameEventArgs e)
            {
                base.OnRenderFrame(e);
                FPSManager.Aggregate(e.Time);
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                Input.Input.Update();

                _elffyWindow.FrameRendering?.Invoke(new Core.FrameEventArgs(e.Time));

                DebugManager.Dump();
                SwapBuffers();
            }

            protected override void OnClosed(EventArgs e)
            {
                base.OnClosed(e);
                _elffyWindow.Closed?.Invoke(this, e);
            }

            public override void Dispose()
            {
                base.Dispose();
            }
        }
        #endregion
    }
}
