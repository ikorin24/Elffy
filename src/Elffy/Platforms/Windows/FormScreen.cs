#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Elffy.Platforms.Windows
{
    public class FormScreen : GLControl
    {
        public bool IsRunning { get; private set; }

        public FormScreen() : base()
        {
        }

        public void Run()
        {
            if(IsDesignMode) { return; }
            IsRunning = true;
            GL.ClearColor(Color.Black);
            GL.Enable(EnableCap.DepthTest);
            SetProjection();
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if(!IsRunning) { return; }
            SetProjection();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if(!IsRunning) { return; }

            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.MatrixMode(MatrixMode.Modelview);
            Matrix4 modelview = Matrix4.LookAt(Vector3.Zero, Vector3.UnitZ, Vector3.UnitY);
            GL.LoadMatrix(ref modelview);

            GL.Begin(PrimitiveType.Quads);

            GL.Color4(Color4.White);
            GL.Vertex3(-1.0f, 1.0f, 4.0f);
            GL.Color4(Color4.Red);
            GL.Vertex3(-1.0f, -1.0f, 4.0f);
            GL.Color4(Color4.Lime);
            GL.Vertex3(1.0f, -1.0f, 4.0f);
            GL.Color4(Color4.Blue);
            GL.Vertex3(1.0f, 1.0f, 4.0f);

            GL.End();
            SwapBuffers();
        }

        private void SetProjection()
        {
            GL.Viewport(0, 0, Width, Height);
            GL.MatrixMode(MatrixMode.Projection);
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView((float)Math.PI / 4, (float)Width / (float)Height, 1.0f, 64.0f);
            GL.LoadMatrix(ref projection);
        }
    }
}
