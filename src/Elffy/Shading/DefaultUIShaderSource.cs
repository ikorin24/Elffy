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

            ref var fixedArea = ref target.TextureFixedArea;

            ref readonly var texture = ref target.Texture;
            var hasTexture = texture.IsEmpty == false;
            uniform.SendTexture2D("tex_sampler", texture, TextureUnitNumber.Unit0);
            uniform.Send("hasTexture", hasTexture);

            if(hasTexture) {
                var controlSize = (Vector2)target.ActualSize;
                var textureSize = (Vector2)target.ActualTextureSize;
                var lt = new Vector2(fixedArea.Left, fixedArea.Top);
                var rb = new Vector2(fixedArea.Right, fixedArea.Bottom);

                var r = controlSize / textureSize;
                var a = lt / controlSize;
                var b = Vector2.One - (rb / controlSize);
                var p = lt / textureSize;
                var q = Vector2.One - (rb / textureSize);
                var z = ((textureSize - lt - rb) * controlSize) / ((controlSize - lt - rb) * textureSize);

                uniform.Send("r", r);
                uniform.Send("a", a);
                uniform.Send("b", b);
                uniform.Send("p", p);
                uniform.Send("q", q);
                uniform.Send("z", z);
            }
            uniform.Send("back", target.Background);
        }

        private const string VertSource =
@"#version 410
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
@"#version 410
in vec2 UV;
out vec4 fragColor;
uniform vec2 r;
uniform vec2 a;
uniform vec2 b;
uniform vec2 p;
uniform vec2 q;
uniform vec2 z;
uniform sampler2D tex_sampler;
uniform bool hasTexture;
uniform vec4 back;

vec2 Filter(vec2 start, vec2 end, vec2 value)
{
    return vec2(step(start.x, value.x) * (1 - step(end.x, value.x)),
                step(start.y, value.y) * (1 - step(end.y, value.y)));
}

void main()
{
    if(hasTexture) {
        vec2 uv1 = Filter(vec2(0.0, 0.0), a, UV) * UV * r;
        vec2 uv2 = Filter(a, b, UV) * (p + (UV - a) * z);
        vec2 uv3 = Filter(b, vec2(1.0, 1.0), UV) * (q + (UV - b) * r);
        vec2 uv = uv1 + uv2 + uv3;
        vec4 t = texture(tex_sampler, uv);
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
