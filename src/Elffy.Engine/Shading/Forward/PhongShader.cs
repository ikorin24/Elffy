#nullable enable
using Elffy.Graphics.OpenGL;
using System;

namespace Elffy.Shading.Forward
{
    public sealed class PhongShader : ShaderSource
    {
        private const float DefaultAFactor = 0.8f;
        private const float DefaultDFactor = 0.35f;
        private const float DefaultSFactor = 0.5f;
        private const float DefaultShininess = 10f;

        private Color3 _ambient;
        private Color3 _diffuse;
        private Color3 _specular;
        private float _shininess;
        private ShaderTextureSelector<PhongShader>? _textureSelector;
        private StaticLightManager? _staticLights;

        protected override string VertexShaderSource => VertSource;

        protected override string FragmentShaderSource => FragSource;

        public ref Color3 Ambient => ref _ambient;
        public ref Color3 Diffuse => ref _diffuse;
        public ref Color3 Specular => ref _specular;
        public float Shininess
        {
            get => _shininess;
            set => _shininess = value;
        }
        public ShaderTextureSelector<PhongShader>? TextureSelector
        {
            get => _textureSelector;
            set => _textureSelector = value;
        }

        public PhongShader(ShaderTextureSelector<PhongShader>? textureSelector = null) : this(Color3.White, textureSelector)
        {
        }

        public PhongShader(Color3 color, ShaderTextureSelector<PhongShader>? textureSelector = null)
        {
            _ambient = new Color3(color.R * DefaultAFactor, color.G * DefaultAFactor, color.B * DefaultAFactor);
            _diffuse = new Color3(color.R * DefaultDFactor, color.G * DefaultDFactor, color.B * DefaultDFactor);
            _specular = new Color3(color.R * DefaultSFactor, color.G * DefaultSFactor, color.B * DefaultSFactor);
            _shininess = DefaultShininess;
            _textureSelector = textureSelector;
        }

        public PhongShader(Color3 ambient, Color3 diffuse, Color3 specular, float shininess, ShaderTextureSelector<PhongShader>? textureSelector = null)
        {
            _ambient = ambient;
            _diffuse = diffuse;
            _specular = specular;
            _shininess = shininess;
            _textureSelector = textureSelector;
        }

        public void SetColor(Color3 color)
        {
            _ambient = new Color3(color.R * DefaultAFactor, color.G * DefaultAFactor, color.B * DefaultAFactor);
            _diffuse = new Color3(color.R * DefaultDFactor, color.G * DefaultDFactor, color.B * DefaultDFactor);
            _specular = new Color3(color.R * DefaultSFactor, color.G * DefaultSFactor, color.B * DefaultSFactor);
        }

        protected override void DefineLocation(VertexDefinition definition, Renderable target, Type vertexType)
        {
            definition.Map(vertexType, "vPos", VertexSpecialField.Position);
            definition.Map(vertexType, "vNormal", VertexSpecialField.Normal);
            definition.Map(vertexType, "vUV", VertexSpecialField.UV);
        }

        protected override void SendUniforms(Uniform uniform, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            uniform.Send("ma", _ambient);
            uniform.Send("md", _diffuse);
            uniform.Send("ms", _specular);
            uniform.Send("shininess", _shininess);

            uniform.Send("projection", projection);
            uniform.Send("view", view);
            uniform.Send("modelView", view * model);

            var selector = _textureSelector ?? DefaultShaderTextureSelector<PhongShader>.Default;
            var hasTexture = selector.Invoke(this, target, out var texObj);
            uniform.SendTexture2D("tex_sampler", texObj, TextureUnitNumber.Unit0);
            uniform.Send("hasTexture", hasTexture);

            var staticLights = _staticLights;
            if (staticLights == null) {
                if(target.TryGetHostScreen(out var screen) == false) { throw new InvalidOperationException(); }
                staticLights = screen.Lights.StaticLights;
                _staticLights = staticLights;
            }
            var (lColor, lPos, lightCount) = staticLights.GetBufferData();
            uniform.Send("lightCount", lightCount);
            uniform.SendTexture1D("lColorSampler", lColor, TextureUnitNumber.Unit1);
            uniform.SendTexture1D("lPosSampler", lPos, TextureUnitNumber.Unit2);
        }

        private const string VertSource =
@"#version 410

in vec3 vPos;
in vec3 vNormal;
in vec2 vUV;
out vec3 Pos;
out vec3 Normal;
out vec2 UV;

uniform mat4 modelView;
uniform mat4 projection;

void main()
{
    UV = vUV;
    Pos = vPos;
    Normal = vNormal;
    gl_Position = projection * modelView * vec4(vPos, 1.0);
}
";

        private const string FragSource =
@"#version 410

in vec2 UV;
in vec3 Pos;
in vec3 Normal;
out vec4 fragColor;

uniform mat4 modelView;
uniform mat4 view;
uniform mat4 projection;
uniform int lightCount;
uniform sampler1D lPosSampler;
uniform sampler1D lColorSampler;
uniform vec3 ma;
uniform vec3 md;
uniform vec3 ms;
uniform float shininess;

uniform sampler2D tex_sampler;
uniform bool hasTexture;

void main()
{
    vec3 posView = (modelView * vec4(Pos, 1.0)).xyz;                    // vertex pos in eye space
    vec3 normalView = transpose(inverse(mat3(modelView))) * Normal;    // normal in eye space
    vec3 lightColor = vec3(0, 0, 0);
    for(int i = 0; i < lightCount; i++) {
        vec4 lPosView = view * texelFetch(lPosSampler, i, 0);
        vec3 L = (lPosView.w == 0.0) ? normalize(lPosView.xyz) : normalize(lPosView.xyz / lPosView.w - posView);
        vec3 N = normalize(normalView);
        vec3 R = reflect(-L, N);
        vec3 V = normalize(-posView);
        vec3 l = texelFetch(lColorSampler, i, 0).rgb;
        vec3 la = l * 0.8;
        vec3 ld = l * 0.8;
        vec3 ls = l * 0.2;
        vec3 color = (la * ma) + (ld * md * dot(N, L)) + (ls * ms * max(pow(max(0.0, dot(R, V)), shininess), 0.0));
        lightColor += color;
    }
    fragColor = hasTexture ? vec4(lightColor, 1.0) * texture(tex_sampler, UV)
                           : vec4(lightColor, 1.0);
}
";
    }
}
