#nullable enable
using System;

namespace Elffy.Shading
{
    internal sealed class RenderShadowMapShader : IRenderingShader
    {
        private int _sourceHashCache;

        public static RenderShadowMapShader Instance { get; } = new RenderShadowMapShader();

        string IRenderingShader.VertexShaderSource => Vert;

        string IRenderingShader.FragmentShaderSource => Frag;

        public string? GeometryShaderSource => null;

        private RenderShadowMapShader()
        {
        }

        int IRenderingShader.GetSourceHash()
        {
            if(_sourceHashCache == 0) {
                _sourceHashCache = HashCode.Combine(Vert, Frag);
            }
            return _sourceHashCache;
        }

        public void DefineLocation(VertexDefinition definition, Renderable target, Type vertexType)
        {
            definition.Map(vertexType, "_vPos", VertexSpecialField.Position);
        }

        public void DispatchShader(ShaderDataDispatcher dispatcher, in Matrix4 model, in Matrix4 lightViewProjection)
        {
            dispatcher.SendUniform("_lmvp", lightViewProjection * model);
        }

        void IRenderingShader.InvokeOnProgramDisposed() { }     // nop

        private const string Vert =
@"#version 410
in vec3 _vPos;
uniform mat4 _lmvp;
void main()
{
    gl_Position = _lmvp * vec4(_vPos, 1.0);
}
";

        private const string Frag =
@"#version 410
void main(){}";
    }
}
