#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elffy.Core;
using Elffy.OpenGL;
using Elffy.Shading;

namespace Elffy.UI
{
    /// <summary><see cref="UI.Control"/> を描画するためのオブジェクト。対象の <see cref="UI.Control"/> のインスタンスと一対一の関係を持つ</summary>
    internal sealed class UIRenderable : Renderable
    {
        /// <summary>このインスタンスの描画対象である論理 UI コントロール</summary>
        public Control Control { get; private set; }

        /// <summary><see cref="UI.Control"/> の描画オブジェクトを作成します。</summary>
        /// <param name="control">描画対象の論理 UI コントロール</param>
        public UIRenderable(Control control)
        {
            Debug.Assert(control is null == false);
            Control = control;
            IsFrozen = true;        // disable calling update method per frame
            Shader = UIShaderSource.Instance;
        }

        [SkipLocalsInit]
        protected override void OnActivated()
        {
            base.OnActivated();
            Debug.Assert(InternalLayer is UILayer);

            //     Position                    UI
            //                          
            //     p3(0,1,0) -- p2(1,1,0)     (0,1) --- (1,1)
            //     |         /     |           |      /   |
            //     |        /      |           |     /    |
            //     |       /       |           |    /     |
            //     |      /        |           |   /      |
            //     p0(0,0,0) -- p1(1,0,0)     (0,0) --- (1,0)
            //  Y
            //  |  Z (direction to back of screen)
            //  | /
            //  o ---> X
            //
            // Indices
            // [0, 2, 1], [2, 0, 3]
            //
            // Y axis is inversed on rendered.

            // Build polygons and load them
            ReadOnlySpan<VertexSlim> vertices = stackalloc VertexSlim[4]
            {
                new(new(0, 0, 0), new(0, 0)),
                new(new(1, 0, 0), new(1, 0)),
                new(new(1, 1, 0), new(1, 1)),
                new(new(0, 1, 0), new(0, 1)),
            };
            ReadOnlySpan<int> indices = stackalloc int[6] { 0, 2, 1, 2, 0, 3 };
            LoadGraphicBuffer(vertices, indices);
        }

        protected override void OnRendering(in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            var control = Control;
            var offset = control.Offset;
            var hasOffset = offset != Vector2i.Zero;

            // Add offset translation to the model matrix.
            // Overwrite it instead of creating a new matrix. (It's faster)
            if(hasOffset) {
                Unsafe.AsRef(model).M03 += offset.X;
                Unsafe.AsRef(model).M13 += offset.Y;
            }
            try {
                VAO.Bind(VAO);
                IBO.Bind(IBO);
                ShaderProgram!.Apply(this, in model, in view, in projection);
                DrawElements(IBO.Length, 0);
                VAO.Unbind();
                IBO.Unbind();
            }
            finally {
                // Restore the model matrix for safety.
                // (Though this is no needed I think.)
                if(hasOffset) {
                    Unsafe.AsRef(model).M03 -= offset.X;
                    Unsafe.AsRef(model).M13 -= offset.Y;
                }
            }
        }
    }
}
