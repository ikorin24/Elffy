#nullable enable
using Elffy.Graphics.OpenGL;
using Elffy.UI;

namespace Elffy.Shading.Forward
{
    public sealed class CustonUIShader : UIShaderSource
    {
        private ShaderTextureSelector<PhongShader>? _textureSelector;
        private Vector4 _cornerRadius;

        protected override string VertexShaderSource => VertSource;

        protected override string FragmentShaderSource => FragSource;

        public ShaderTextureSelector<PhongShader>? TextureSelector
        {
            get => _textureSelector;
            set => _textureSelector = value;
        }

        public ref Vector4 CornerRadius => ref _cornerRadius;

        public CustonUIShader(ShaderTextureSelector<PhongShader>? textureSelector = null)
        {

        }

        protected override void DefineLocation(VertexDefinition<VertexSlim> definition, Control target)
        {
            definition.Map("vPos", nameof(VertexSlim.Position));
            definition.Map("vUV", nameof(VertexSlim.UV));
        }

        protected override void SendUniforms(Uniform uniform, Control target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            var size = target.ActualSize;
            uniform.Send("_cornerRadius", _cornerRadius);

            var mvp = projection * view * model;
            uniform.Send("mvp", mvp);

            ref readonly var texture = ref target.Texture;
            var hasTexture = !texture.IsEmpty;
            uniform.Send("hasTexture", hasTexture);
            if(hasTexture) {
                uniform.SendTexture2D("tex_sampler", texture.Texture, TextureUnitNumber.Unit0);
                var uvScale = size / (Vector2)texture.Size;
                uniform.Send("uvScale", uvScale);
            }
            uniform.Send("back", target is Button button && button.IsKeyPressed ? Color4.Aquamarine : target.IsMouseOver ? Color4.MediumAquamarine : target.Background);
            uniform.Send("_size", size);
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
uniform sampler2D tex_sampler;
uniform vec2 uvScale;
uniform bool hasTexture;
uniform vec4 back;
uniform vec4 _cornerRadius;
uniform vec2 _size;

float CalcArea(vec4 r)
{
    vec2 p = UV * _size;
    vec2[4] center = vec2[]
    (
        vec2(r[0], r[0]),
        vec2(_size.x - r[1], r[1]),
        vec2(_size.x - r[2], _size.y - r[2]),
        vec2(r[3], _size.y - r[3])
    );
    vec4 isCorner = vec4
    (
        (1.0 - step(center[0].x, p.x)) * (1.0 - step(center[0].y, p.y)),
        (1.0 - step(p.x, center[1].x)) * (1.0 - step(center[1].y, p.y)),
        (1.0 - step(p.x, center[2].x)) * (1.0 - step(p.y, center[2].y)),
        (1.0 - step(center[3].x, p.x)) * (1.0 - step(p.y, center[3].y))
    );
    vec4 l = vec4
    (
        length(p - center[0]),
        length(p - center[1]),
        length(p - center[2]),
        length(p - center[3])
    );
    vec4 a = clamp(-l + r + 0.5, 0.0, 1.0);
    return dot(a, isCorner) + step(isCorner[0] + isCorner[1] + isCorner[2] + isCorner[3], 0.5);
}

void main()
{
    if(hasTexture) {
        vec4 t = texture(tex_sampler, UV * uvScale) * vec4(1.0, 1.0, 1.0, CalcArea(_cornerRadius));
        float tai = 1.0 - t.a;
        fragColor = vec4(t.r * t.a + back.r * tai,
                         t.g * t.a + back.g * tai,
                         t.b * t.a + back.b * tai,
                         t.a + back.a * tai);
    }
    else {
        fragColor = back * vec4(1.0, 1.0, 1.0, CalcArea(_cornerRadius));
    }
}
";
    }
}
