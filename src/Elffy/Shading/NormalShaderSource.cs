#nullable enable
using Elffy.Core;
using OpenTK.Graphics.OpenGL;

namespace Elffy.Shading
{
    public class NormalShaderSource : ShaderSource
    {
        private static NormalShaderSource? _instance;
        internal static NormalShaderSource Instance => _instance ??= new NormalShaderSource();

        private NormalShaderSource() { }

        protected override void DefineLocation(VertexDefinition definition)
        {
            definition.Position("vPos");
            definition.Normal("vNormal");
        }

        protected override string FragmentShaderSource() => FragSource;

        protected override string VertexShaderSource() => VertSource;

        protected override void SendUniforms(Uniform uniform, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            uniform.Send("model", model);
            uniform.Send("view", view);
            uniform.Send("projection", projection);
        }

        private const string VertSource =
@"#version 440

in vec3 vPos;
in vec3 vNormal;
out vec3 color;
uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    gl_Position = projection * view * model * vec4(vPos, 1.0);
    color = normalize(vNormal);
}	

";

        private const string FragSource =
@"#version 440

in vec3 color;
out vec4 fragColor;

void main()
{
    fragColor = vec4(abs(color), 1.0);
}
";
    }
}
