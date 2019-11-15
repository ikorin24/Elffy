#nullable enable
using Elffy.Core;
using System.Linq;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;

namespace Elffy
{
    /// <summary><see cref="FrameObject"/> のレイヤークラス</summary>
    [DebuggerDisplay("Layer: {Name} (ObjectCount = {ObjectCount})", Type = nameof(Layer), TargetTypeName = nameof(Layer))]
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

        /// <summary>画面への投影行列を指定して、描画を実行します</summary>
        /// <param name="projection"></param>
        internal void Render(Matrix4 projection) => Render(projection, Matrix4.Identity);

        /// <summary>画面への投影行列とカメラ行列を指定して、描画を実行します</summary>
        /// <param name="projection">投影行列</param>
        /// <param name="view">カメラ行列</param>
        internal void Render(Matrix4 projection, Matrix4 view)
        {
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadMatrix(ref projection);
            GL.MatrixMode(MatrixMode.Modelview);
            foreach(var renderable in Renderables.Where(x => x.IsRoot && x.IsVisible)) {
                GL.LoadMatrix(ref view);
                renderable.Render();
            }
        }
    }
}
