#nullable enable
using Elffy.Core;
using Elffy.Diagnostics;

namespace Elffy.Shading
{
    [ShaderTargetVertexType(typeof(VertexSlim))]
    internal sealed class UISolidColorShaderSource : ShaderSource
    {
        private Color4 _color;

        protected override string VertexShaderSource => VertSource;

        protected override string FragmentShaderSource => FragSource;

        public ref Color4 Color => ref _color;

        public UISolidColorShaderSource(in Color4 color)
        {
            _color = color;
        }

        protected override void DefineLocation(VertexDefinition definition, Renderable target)
        {
            definition.Map<VertexSlim>("vPos", nameof(VertexSlim.Position));
        }

        protected override void SendUniforms(Uniform uniform, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            var mvp = projection * view * model;
            uniform.Send("mvp", mvp);
            uniform.Send("solidColor", _color);
        }

        private const string VertSource =
@"#version 440

in vec3 vPos;
uniform mat4 mvp;

void main()
{
    gl_Position = mvp * vec4(vPos, 1.0);
}
";

        private const string FragSource =
@"#version 440

uniform vec4 solidColor;
out vec4 fragColor;

void main()
{
    fragColor = solidColor;
}
";
    }
}
