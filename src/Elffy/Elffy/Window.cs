using OpenTK;
using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using Elffy.UI;

namespace Elffy
{
    public class Window : GameWindow, IGameScreen
    {
        private readonly RenderingArea _renderingArea = new RenderingArea();

        public IUIRoot UIRoot => _renderingArea.UIRoot;

        public event EventHandler Initialized
        {
            add { _renderingArea.Initialized += value; }
            remove { _renderingArea.Initialized -= value; }
        }
        public event EventHandler Rendering;
        public event EventHandler Rendered;

        public Window() : this(800, 450, "Window", WindowStyle.Default) { }

        public Window(WindowStyle windowStyle) : this(800, 450, "Window", windowStyle) { }

        public Window(int width, int height, string title, WindowStyle windowStyle) : base(width, height, GraphicsMode.Default, title, (GameWindowFlags)windowStyle)
        {
            VSync = VSyncMode.On;
            TargetRenderFrequency = DisplayDevice.Default.RefreshRate;
        }

        public bool AddFrameObject(FrameObject frameObject) => _renderingArea.AddFrameObject(frameObject);

        public bool RemoveFrameObject(FrameObject frameObject) => _renderingArea.RemoveFrameObject(frameObject);

        public FrameObject FindObject(string tag) => _renderingArea.FindObject(tag);

        public List<FrameObject> FindAllObject(string tag) => _renderingArea.FindAllObject(tag);

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _renderingArea.Initialize();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            _renderingArea.Size = ClientSize;
        }

        protected override void OnRenderFrame(OpenTK.FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            Rendering?.Invoke(this, EventArgs.Empty);
            _renderingArea.RenderFrame();
            Rendered?.Invoke(this, EventArgs.Empty);
            SwapBuffers();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _renderingArea.Clear();
        }
    }
}
