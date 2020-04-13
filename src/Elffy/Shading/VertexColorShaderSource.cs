#nullable enable
using Elffy.Core;

namespace Elffy.Shading
{
    public class VertexColorShaderSource : ShaderSource
    {
        private static VertexColorShaderSource? _instance;
        internal static VertexColorShaderSource Instance => _instance ??= new VertexColorShaderSource();

        private VertexColorShaderSource()
        {
        }

        protected override string VertexShaderSource() => VertexShader;

        protected override string FragmentShaderSource() => FragmentShader;

        protected override void DefineLocation(VertexDefinition definition)
        {
            definition.Position("pos");
            definition.Color("vertexColor");
        }

        protected override void SendUniforms(Uniform uniform, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            uniform.Send("modelViewProjection", projection * view * model);
        }

        private const string VertexShader =
@"#version 440

in vec3 pos;
in vec4 vertexColor;
out vec4 color;

uniform mat4 modelViewProjection;

void main()
{
    gl_Position = modelViewProjection * vec4(pos, 1.0);
    color = vertexColor;
}
";

        private const string FragmentShader =
@"#version 440

in vec4 color;
out vec4 fragColor;

void main()
{
    fragColor = color;
}
";
    }
}
