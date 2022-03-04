#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using Elffy.Graphics.OpenGL;
using Elffy.Shading;

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
            Activating.Subscribe((f, ct) => SafeCast.As<UIRenderable>(f).OnActivating());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetUILayer([MaybeNullWhen(false)] out UILayer layer)
        {
            layer = SafeCast.As<UILayer>(Layer);
            return layer is not null;
        }

        internal void DoUIEvent() => _control.DoUIEvent();

        [SkipLocalsInit]
        private unsafe UniTask OnActivating()
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
            var rt = control.RenderTransform;
            var shader = SafeCast.NotNullAs<UIRenderingShader>(ShaderInternal);
            var program = RendererData.GetValidProgram();
            if(IsLoaded == false) {
                throw new InvalidOperationException("Not loaded yet.");
            }

            VAO.Bind(VAO);
            IBO.Bind(IBO);
            ProgramObject.UseProgram(program);
            if(rt.IsIdentity) {
                shader.OnRenderingInternal(program, control, model, view, projection);

            }
            else {
                // TODO:
                var modelTransformed = model;
                //ref var rto = ref control.RenderTransformOrigin;

                shader.OnRenderingInternal(program, control, modelTransformed, view, projection);
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
