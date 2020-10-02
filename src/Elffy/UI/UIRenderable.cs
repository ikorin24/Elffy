#nullable enable
using System;
using System.Diagnostics;
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

        protected override void OnAlive()
        {
            base.OnAlive();
            Debug.Assert(InternalLayer is UILayer);

            // Build polygons and load them
            var control = Control;
            var p0 = new Vector3(control.OffsetX, control.OffsetY, 0);
            var p1 = p0 + new Vector3(control.Width, 0, 0);
            var p2 = p0 + new Vector3(control.Width, control.Height, 0);
            var p3 = p0 + new Vector3(0, control.Height, 0);
            ReadOnlySpan<VertexSlim> vertices = stackalloc VertexSlim[4]
            {
                new VertexSlim(p0, new Vector2(0, 1)),
                new VertexSlim(p1, new Vector2(1, 1)),
                new VertexSlim(p2, new Vector2(1, 0)),
                new VertexSlim(p3, new Vector2(0, 0)),
            };
            ReadOnlySpan<int> indices = stackalloc int[6] { 0, 2, 1, 2, 0, 3 };
            LoadGraphicBuffer(vertices, indices);
        }

        protected override void OnRendering(in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            VAO.Bind(VAO);
            IBO.Bind(IBO);
            ShaderProgram!.Apply(this, Span<Light>.Empty, in model, in view, in projection);
            DrawElements(IBO.Length, 0);
            VAO.Unbind();
            IBO.Unbind();
        }
    }
}
