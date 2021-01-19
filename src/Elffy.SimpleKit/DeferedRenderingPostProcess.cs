#nullable enable
using Elffy.OpenGL;
using Elffy.Shading;

namespace Elffy
{
    public sealed class DeferedRenderingPostProcess : PostProcess
    {
        private readonly GBuffer _gBuffer;

        public override string FragShaderSource => FragSource;

        internal DeferedRenderingPostProcess(GBuffer gBuffer)
        {
            _gBuffer = gBuffer;
        }

        protected override void SendUniforms(Uniform uniform, in Vector2i screenSize)
        {
            uniform.SendTexture2D("_posSampler", _gBuffer.Position, TextureUnitNumber.Unit0);
            uniform.SendTexture2D("_normalSampler", _gBuffer.Normal, TextureUnitNumber.Unit1);
            uniform.SendTexture2D("_colorSampler", _gBuffer.Color, TextureUnitNumber.Unit2);
        }

        private const string FragSource =
@"#version 440
in vec2 _uv;
uniform sampler2D _posSampler;
uniform sampler2D _normalSampler;
uniform sampler2D _colorSampler;
out vec4 _fragColor;

void main()
{
    vec3 pos = texture(_posSampler, _uv).rgb;
    vec3 normal = texture(_normalSampler, _uv).rgb;
    vec4 color = texture(_colorSampler, _uv).rgba;
    vec3 albedo = color.rgb;
    float specular = color.a;

    // TODO: this is just a sample
    _fragColor = vec4(albedo, 1.0);
}
";
    }
}
