#nullable enable
using Elffy.Core;
using Elffy.Diagnostics;
using Elffy.OpenGL;
using Elffy.UI;

namespace Elffy.Shading
{
    [ShaderTargetVertexType(typeof(VertexSlim))]
    internal sealed class DefaultUIShaderSource : ShaderSource
    {
        public override string VertexShaderSource => VertSource;

        public override string FragmentShaderSource => FragSource;

        public static readonly DefaultUIShaderSource Instance = new DefaultUIShaderSource();

        private DefaultUIShaderSource()
        {
        }

        protected override void DefineLocation(VertexDefinition definition, Renderable target)
        {
            definition.Map<VertexSlim>("vPos", nameof(VertexSlim.Position));
            definition.Map<VertexSlim>("vUV", nameof(VertexSlim.UV));
        }

        protected override void SendUniforms(Uniform uniform, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            var control = SafeCast.As<UIRenderable>(target).Control;
            var mvp = projection * view * model;
            uniform.Send("mvp", mvp);

            ref readonly var texture = ref control.Texture;
            uniform.SendTexture2D("tex_sampler", texture.Texture, TextureUnitNumber.Unit0);
            uniform.Send("hasTexture", texture.IsEmpty == false);

            var uvScale = (Vector2)control.Size / texture.Size;
            uniform.Send("uvScale", uvScale);
            uniform.Send("back", control.Background);
        }

        private const string VertSource =
@"#version 440

in vec3 vPos;
in vec2 vUV;
out vec2 UV;

uniform vec2 uvScale;
uniform mat4 mvp;

void main()
{
    UV = vUV * uvScale;
    gl_Position = mvp * vec4(vPos, 1.0);
}
";

        private const string FragSource =
@"#version 440

in vec2 UV;
out vec4 fragColor;
uniform sampler2D tex_sampler;
uniform bool hasTexture;
uniform vec4 back;

void main()
{
    if(hasTexture) {
        vec4 t = texture(tex_sampler, UV);
        float tai = 1.0 - t.a;
        fragColor = vec4(t.r * t.a + back.r * tai,
                         t.g * t.a + back.g * tai,
                         t.b * t.a + back.b * tai,
                         t.a + back.a * tai);
    }
    else {
        fragColor = back;
    }
}
";
    }
}
