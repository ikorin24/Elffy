#nullable enable
using Elffy.OpenGL;

namespace Elffy.Shading
{
    public sealed class DeferedRenderingPostProcess : PostProcess
    {
        private readonly GBuffer _gBuffer;
        private Matrix4 _view;
        private Matrix4 _projection;

        public override string FragShaderSource => FragSource;

        internal DeferedRenderingPostProcess(GBuffer gBuffer)
        {
            _gBuffer = gBuffer;
        }

        public void SetMatrices(in Matrix4 view, in Matrix4 projection)
        {
            _view = view;
            _projection = projection;
        }

        protected override void SendUniforms(Uniform uniform, in Vector2i screenSize)
        {
            uniform.Send("_projection", _projection);
            uniform.Send("_view", _view);
            uniform.SendTexture2D("_posSampler", _gBuffer.Position, TextureUnitNumber.Unit0);
            uniform.SendTexture2D("_normalSampler", _gBuffer.Normal, TextureUnitNumber.Unit1);
            uniform.SendTexture2D("_colorSampler", _gBuffer.Color, TextureUnitNumber.Unit2);

            uniform.SendTexture1D("_lightPosSampler", _gBuffer.LightPositions, TextureUnitNumber.Unit3);
            uniform.SendTexture1D("_lightColorSampler", _gBuffer.Lights, TextureUnitNumber.Unit4);

            uniform.Send("_ma", new Color3(0.8f));
            uniform.Send("_md", new Color3(0.35f));
            uniform.Send("_ms", new Color3(0.5f));
            uniform.Send("_shininess", 10f);
            uniform.Send("_lPos", new Vector4(0, 1, 0, 0));
            uniform.Send("_la", new Vector3(0.8f));
            uniform.Send("_ld", new Vector3(0.8f));
            uniform.Send("_ls", new Vector3(0.2f));
        }

        private const string FragSource =
@"#version 440
in vec2 _uv;
uniform mat4 _projection;
uniform mat4 _view;
uniform sampler2D _posSampler;
uniform sampler2D _normalSampler;
uniform sampler2D _colorSampler;
uniform sampler1D _lightPosSampler;
uniform sampler1D _lightColorSampler;

uniform vec4 _lPos;
uniform vec3 _la;
uniform vec3 _ld;
uniform vec3 _ls;
uniform vec3 _ma;
uniform vec3 _md;
uniform vec3 _ms;
uniform float _shininess;

out vec4 _fragColor;

void main()
{
    vec3 pos = texture(_posSampler, _uv).rgb;
    vec3 normal = texture(_normalSampler, _uv).rgb;
    vec3 color = texture(_colorSampler, _uv).rgb;

    //vec4 lPosView = _view * _lPos;       // light pos in eye space
    vec4 lPosView = _view * texelFetch(_lightPosSampler, 0, 0);       // light pos in eye space
    vec3 L = (lPosView.w == 0.0) ? normalize(lPosView.xyz) : normalize(lPosView.xyz / lPosView.w - pos);
    vec3 N = normalize(normal);
    vec3 R = reflect(-L, N);
    vec3 V = normalize(-pos);
    vec3 lightColor = (_la * _ma) + (_ld * _md * dot(N, L)) + (_ls * _ms * max(pow(max(0.0, dot(R, V)), _shininess), 0.0));

    //_fragColor = vec4(normal, 1.0);
    //_fragColor = vec4(normal * 0.5 + vec3(0.5, 0.5, 0.5), 1.0);
    _fragColor = vec4(color * lightColor, 1.0);
}
";
    }
}
