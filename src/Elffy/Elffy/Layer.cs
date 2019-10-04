using Elffy.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Elffy
{
    /// <summary><see cref="FrameObject"/> のレイヤークラス</summary>
    public class Layer : LayerBase
    {
        /// <summary>このレイヤーのライティングを有効にするかどうか</summary>
        public bool IsLightingEnabled { get; set; }

        /// <summary>レイヤー名を指定して <see cref="Layer"/> を作成します</summary>
        /// <param name="name">レイヤー名</param>
        public Layer(string name)
        {
            Name = name;
        }

        #region Render
        /// <summary>画面への投影行列を指定して、描画を実行します</summary>
        /// <param name="projection"></param>
        public void Render(Matrix4 projection) => Render(projection, Matrix4.Identity);

        /// <summary>画面への投影行列とカメラ行列を指定して、描画を実行します</summary>
        /// <param name="projection">投影行列</param>
        /// <param name="view">カメラ行列</param>
        public void Render(Matrix4 projection, Matrix4 view)
        {
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projection);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref view);
            // TODO: OpenGL の行列スタックの深さを確認
            foreach(var renderable in Renderables.Where(x => x.IsVisible)) {
                GL.PushMatrix();
                renderable.Render();
                GL.PopMatrix();
            }
        }
        #endregion
    }
}
