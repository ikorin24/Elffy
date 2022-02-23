#nullable enable
using Elffy.Graphics.OpenGL;
using Elffy.UI;

namespace Elffy.Shading.Forward
{
    internal sealed class DefaultUIShader : UIRenderingShader
    {
        protected override string VertexShaderSource => VertSource;

        protected override string FragmentShaderSource => FragSource;

        public static readonly DefaultUIShader Instance = new DefaultUIShader();

        private DefaultUIShader()
        {
        }

        protected override void DefineLocation(VertexDefinition<VertexSlim> definition, Control target)
        {
            definition.Map("vPos", nameof(VertexSlim.Position));
            definition.Map("vUV", nameof(VertexSlim.UV));
        }

        protected override void OnRendering(ShaderDataDispatcher dispatcher, Control target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            var mvp = projection * view * model;
            dispatcher.SendUniform("mvp", mvp);

            ref readonly var texture = ref target.Texture;
            dispatcher.SendUniformTexture2D("tex_sampler", texture.Texture, TextureUnitNumber.Unit0);
            dispatcher.SendUniform("hasTexture", texture.IsEmpty == false);

            var uvScale = target.ActualSize / (Vector2)texture.Size;
            dispatcher.SendUniform("uvScale", uvScale);
            dispatcher.SendUniform("back", target.Background);
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
        vec4 t = textureLod(tex_sampler, UV, 0);
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
