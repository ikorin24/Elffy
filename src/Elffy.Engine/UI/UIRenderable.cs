#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Elffy.Graphics.OpenGL;

namespace Elffy.UI
{
    // [NOTE]
    // See the comments of Control to know how UIRenderable and Control work.

    /// <summary>Renderable object which render <see cref="UI.Control"/>. <see cref="UIRenderable"/> object has a one-to-one relationship with <see cref="UI.Control"/>.</summary>
    internal sealed class UIRenderable : Renderable
    {
        private readonly Control _control;

        /// <summary>Target control which <see cref="UIRenderable"/> renders.</summary>
        public Control Control => _control;

        public UIRenderable(Control control)
        {
            Debug.Assert(control is not null);
            _control = control;
            IsFrozen = true;        // disable calling update method per frame
        }

        internal void DoUIEvent() => _control.DoUIEvent();

        [SkipLocalsInit]
        protected unsafe sealed override UniTask<AsyncUnit> OnActivating(CancellationToken cancellationToken)
        {
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
            const int VertexCount = 4;
            const int IndexCount = 6;
            var vertices = stackalloc VertexSlim[VertexCount]
            {
                new(new(0, 0, 0), new(0, 0)),
                new(new(1, 0, 0), new(1, 0)),
                new(new(1, 1, 0), new(1, 1)),
                new(new(0, 1, 0), new(0, 1)),
            };
            var indices = stackalloc int[IndexCount] { 0, 2, 1, 2, 0, 3 };
            LoadMesh(vertices, VertexCount, indices, IndexCount);
            return new UniTask<AsyncUnit>(AsyncUnit.Default);
        }

        protected override void OnRendering(in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            var control = Control;
            ref var rt = ref control.RenderTransform;

            VAO.Bind(VAO);
            IBO.Bind(IBO);
            if(rt.IsIdentity) {
                ShaderProgram!.ApplyForUI(in model, in view, in projection);
            }
            else {
                // TODO: 実装
                var modelTransformed = model;
                //ref var rto = ref control.RenderTransformOrigin;
                ShaderProgram!.ApplyForUI(in modelTransformed, in view, in projection);
            }
            DrawElements(0, IBO.Length);
            VAO.Unbind();
            IBO.Unbind();
        }

        protected override void OnDead()
        {
            base.OnDead();
            _control.OnRenderableDead();
        }
    }
}
