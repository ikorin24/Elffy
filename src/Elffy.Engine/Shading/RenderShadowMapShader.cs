#nullable enable
using System;

namespace Elffy.Shading
{
    internal sealed class RenderShadowMapShader : IRenderingShader
    {
        public static RenderShadowMapShader Instance { get; } = new RenderShadowMapShader();

        private RenderShadowMapShader()
        {
        }

        public void DefineLocation(VertexDefinition definition, Renderable target, Type vertexType)
        {
            definition.Map(vertexType, 0, VertexSpecialField.Position);
        }

        public void DispatchShader(ShaderDataDispatcher dispatcher, in Matrix4 model, in Matrix4 lightViewProjection)
        {
            dispatcher.SendUniform("_lmvp", lightViewProjection * model);
        }

        void IRenderingShader.OnProgramDisposedInternal() { }     // nop

        void IRenderingShader.OnAttachedInternal(Renderable target) { }   // nop

        void IRenderingShader.OnDetachedInternal(Renderable target) { }   // nop

        ShaderSource IRenderingShader.GetShaderSourceInternal(Renderable target, ObjectLayer layer) => new()
        {
            VertexShader =
            """
            #version 410
            layout (location = 0) in vec3 _vPos;
            uniform mat4 _lmvp;
            void main()
            {
                gl_Position = _lmvp * vec4(_vPos, 1.0);
            }
            """,
            FragmentShader =
            """
            #version 410
            void main(){}
            """,
        };
    }
}
