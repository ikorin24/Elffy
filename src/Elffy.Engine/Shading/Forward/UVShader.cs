#nullable enable
using System;

namespace Elffy.Shading.Forward
{
    public sealed class UVShader : RenderingShader
    {
        protected override void DefineLocation(VertexDefinition definition, Renderable target, Type vertexType)
        {
            definition.Map(vertexType, "_pos", VertexSpecialField.Position);
            definition.Map(vertexType, "_uv", VertexSpecialField.UV);
        }

        protected override void OnRendering(ShaderDataDispatcher dispatcher, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            dispatcher.SendUniform("_mvp", projection * view * model);
        }

        protected override ShaderSource GetShaderSource(Renderable target, ObjectLayer layer)
        {
            return new()
            {
                VertexShader = VertSource,
                FragmentShader = FragSource,
            };
        }

        private const string VertSource =
@"#version 410
in vec3 _pos;
in vec2 _uv;
out vec2 _vUV;
uniform mat4 _mvp;
void main()
{
    _vUV = _uv;
    gl_Position = _mvp * vec4(_pos, 1.0);
}
";
        private const string FragSource =
@"#version 410
in vec2 _vUV;
out vec4 _fragColor;
void main()
{
    _fragColor = vec4(_vUV, 0.0, 1.0);
}
";
    }
}
