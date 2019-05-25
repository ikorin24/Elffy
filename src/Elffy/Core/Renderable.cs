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

        public Material Material { get; set; }

        internal void Render()
        {
            // 座標を適用
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref _modelView);

            // マテリアルの適用
            Material?.Apply();

            // 頂点を描画
            TextureVertex();
        }

        protected abstract void TextureVertex();
    }
}
