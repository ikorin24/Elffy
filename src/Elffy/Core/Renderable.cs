using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL;

namespace Elffy.Core
{
    public abstract class Renderable : Positionable
    {
        /// <summary>描画処理を行うかどうか</summary>
        public bool IsVisible { get; set; } = true;

        //public abstract void Render();
        internal void Render()
        {
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref _modelView);
            TextureVertex();
        }

        protected abstract void TextureVertex();
    }
}
