#nullable enable
using System;
using System.Diagnostics;
using Elffy.Components;
using Elffy.Core;
using Elffy.Diagnostics;
using Elffy.OpenGL;
using Elffy.UI;

namespace Elffy.Shading
{
    [ShaderTargetVertexType(typeof(Vertex))]
    internal sealed class UIShaderSource : ShaderSource
    {
        protected override string VertexShaderSource => VertSource;

        protected override string FragmentShaderSource => FragSource;

        public static readonly UIShaderSource Instance = new UIShaderSource();

        private UIShaderSource()
        {
        }

        protected override void DefineLocation(VertexDefinition definition, Renderable target)
        {
            definition.Map<Vertex>(nameof(Vertex.Position), "vPos");
            definition.Map<Vertex>(nameof(Vertex.TexCoord), "vUV");
        }

        protected override void SendUniforms(Uniform uniform, Renderable target, ReadOnlySpan<Light> lights, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            Debug.Assert(target is UIRenderable);
            var mvp = projection * view * model;
            uniform.Send("mvp", mvp);
            uniform.Send("tex_sampler", TextureUnitNumber.Unit0);
        }

        private const string VertSource =
@"#version 440

in vec3 vPos;
in vec2 vUV;
out vec2 UV;

uniform mat4 mvp;

void main()
{
    UV = vUV;
    gl_Position = mvp * vec4(vPos, 1.0);
}
";

        private const string FragSource =
@"#version 440

in vec2 UV;
out vec4 fragColor;
uniform sampler2D tex_sampler;

void main()
{
    fragColor = texture(tex_sampler, UV);
}
";
    }
}
