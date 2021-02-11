#nullable enable
using Elffy.Core;
using Elffy.Diagnostics;
using Elffy.OpenGL;
using Elffy.UI;

namespace Elffy.Shading
{
    [ShaderTargetVertexType(typeof(VertexSlim))]
    internal sealed class DefaultUIShaderSource : UIShaderSource
    {
        public override string VertexShaderSource => VertSource;

        public override string FragmentShaderSource => FragSource;

        public static readonly DefaultUIShaderSource Instance = new DefaultUIShaderSource();

        private DefaultUIShaderSource()
        {
        }

        protected override void DefineLocation(VertexDefinition<VertexSlim> definition, Control target)
        {
            definition.Map("vPos", nameof(VertexSlim.Position));
            definition.Map("vUV", nameof(VertexSlim.UV));
        }

        protected override void SendUniforms(Uniform uniform, Control target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            var mvp = projection * view * model;
            uniform.Send("mvp", mvp);

            ref readonly var texture = ref target.Texture;
            uniform.SendTexture2D("tex_sampler", texture.Texture, TextureUnitNumber.Unit0);
            uniform.Send("hasTexture", texture.IsEmpty == false);

            var uvScale = (Vector2)target.Size / texture.Size;
            uniform.Send("uvScale", uvScale);
            uniform.Send("back", target.Background);
        }

        private const string VertSource =
@"#version 410

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
@"#version 410

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
