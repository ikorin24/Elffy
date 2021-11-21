#nullable enable
using Elffy.Components;
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

        private readonly static PhongShaderTextureSelector _defaultTextureSelector
            = (PhongShader _, Renderable renderable, out TextureObject t) =>
            {
                if(renderable.TryGetComponent<Texture>(out var texture)) {
                    t = texture.TextureObject;
                    return true;
                }
                t = TextureObject.Empty;
                return false;
            };

        private Color3 _ambient;
        private Color3 _diffuse;
        private Color3 _specular;
        private float _shininess;
        private PhongShaderTextureSelector? _textureSelector;

        public override string VertexShaderSource => VertSource;

        public override string FragmentShaderSource => FragSource;

        public ref Color3 Ambient => ref _ambient;
        public ref Color3 Diffuse => ref _diffuse;
        public ref Color3 Specular => ref _specular;
        public float Shininess
        {
            get => _shininess;
            set => _shininess = value;
        }
        public PhongShaderTextureSelector? TextureSelector
        {
            get => _textureSelector;
            set => _textureSelector = value;
        }

        public PhongShader(PhongShaderTextureSelector? textureSelector = null) : this(Color3.White, textureSelector)
        {
        }

        public PhongShader(Color3 color, PhongShaderTextureSelector? textureSelector = null)
        {
            _ambient = new Color3(color.R * DefaultAFactor, color.G * DefaultAFactor, color.B * DefaultAFactor);
            _diffuse = new Color3(color.R * DefaultDFactor, color.G * DefaultDFactor, color.B * DefaultDFactor);
            _specular = new Color3(color.R * DefaultSFactor, color.G * DefaultSFactor, color.B * DefaultSFactor);
            _shininess = DefaultShininess;
            _textureSelector = textureSelector;
        }

        public PhongShader(Color3 ambient, Color3 diffuse, Color3 specular, float shininess, PhongShaderTextureSelector? textureSelector = null)
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

            uniform.Send("model", model);
            uniform.Send("view", view);
            uniform.Send("projection", projection);

            uniform.Send("lPos", new Vector4(0, 1, 0, 0));
            uniform.Send("la", new Vector3(0.8f));
            uniform.Send("ld", new Vector3(0.8f));
            uniform.Send("ls", new Vector3(0.2f));

            var selector = _textureSelector ?? _defaultTextureSelector;
            var hasTexture = selector.Invoke(this, target, out var texObj);
            uniform.SendTexture2D("tex_sampler", texObj, TextureUnitNumber.Unit0);
            uniform.Send("hasTexture", hasTexture);
        }

        private const string VertSource =
@"#version 410

in vec3 vPos;
in vec3 vNormal;
in vec2 vUV;
out vec3 Pos;
out vec3 Normal;
out vec2 UV;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    UV = vUV;
    Pos = vPos;
    Normal = vNormal;
    mat4 modelView = view * model;
    gl_Position = projection * modelView * vec4(vPos, 1.0);
}
";

        private const string FragSource =
@"#version 410

in vec2 UV;
in vec3 Pos;
in vec3 Normal;
out vec4 fragColor;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform vec4 lPos;
uniform vec3 la;
uniform vec3 ld;
uniform vec3 ls;
uniform vec3 ma;
uniform vec3 md;
uniform vec3 ms;
uniform float shininess;

uniform sampler2D tex_sampler;
uniform bool hasTexture;

void main()
{
    mat4 modelView = view * model;
    vec3 posView = (modelView * vec4(Pos, 1.0)).xyz;                    // vertex pos in eye space
    vec3 normalView = transpose(inverse(mat3(modelView))) * Normal;    // normal in eye space
    vec4 lPosView = view * lPos;                                        // light pos in eye space
    vec3 L = (lPosView.w == 0.0) ? normalize(lPosView.xyz) : normalize(lPosView.xyz / lPosView.w - posView);
    vec3 N = normalize(normalView);
    vec3 R = reflect(-L, N);
    vec3 V = normalize(-posView);
    vec3 color = (la * ma) + (ld * md * dot(N, L)) + (ls * ms * max(pow(max(0.0, dot(R, V)), shininess), 0.0));

    fragColor = hasTexture ? vec4(color, 1.0) * texture(tex_sampler, UV)
                           : vec4(color, 1.0);
}
";

    }

    public delegate bool PhongShaderTextureSelector(PhongShader shader, Renderable target, out TextureObject textureObject);
}
