#nullable enable
namespace Elffy.Shading.Forward;

public sealed class PhongShader : RenderingShader
{
    private const float DefaultAFactor = 0.8f;
    private const float DefaultDFactor = 0.35f;
    private const float DefaultSFactor = 0.5f;
    private const float DefaultShininess = 10f;

    private Color3 _ambient;
    private Color3 _diffuse;
    private Color3 _specular;
    private float _shininess;
    private Texture? _texture;

    public ref Color3 Ambient => ref _ambient;
    public ref Color3 Diffuse => ref _diffuse;
    public ref Color3 Specular => ref _specular;
    public float Shininess
    {
        get => _shininess;
        set => _shininess = value;
    }
    public Texture? Texture { get => _texture; set => _texture = value; }

    public PhongShader() : this(Color3.White)
    {
    }

    public PhongShader(Color3 color)
    {
        _ambient = new Color3(color.R * DefaultAFactor, color.G * DefaultAFactor, color.B * DefaultAFactor);
        _diffuse = new Color3(color.R * DefaultDFactor, color.G * DefaultDFactor, color.B * DefaultDFactor);
        _specular = new Color3(color.R * DefaultSFactor, color.G * DefaultSFactor, color.B * DefaultSFactor);
        _shininess = DefaultShininess;
    }

    public PhongShader(Color3 ambient, Color3 diffuse, Color3 specular, float shininess)
    {
        _ambient = ambient;
        _diffuse = diffuse;
        _specular = specular;
        _shininess = shininess;
    }

    public void SetColor(Color3 color)
    {
        _ambient = new Color3(color.R * DefaultAFactor, color.G * DefaultAFactor, color.B * DefaultAFactor);
        _diffuse = new Color3(color.R * DefaultDFactor, color.G * DefaultDFactor, color.B * DefaultDFactor);
        _specular = new Color3(color.R * DefaultSFactor, color.G * DefaultSFactor, color.B * DefaultSFactor);
    }

    protected override void OnProgramDisposed()
    {
        _texture?.Dispose();
        _texture = null;
        base.OnProgramDisposed();
    }

    protected override void DefineLocation(VertexDefinition definition, in LocationDefinitionContext context)
    {
        definition.Map(context.VertexType, "vPos", VertexFieldSemantics.Position);
        definition.Map(context.VertexType, "vNormal", VertexFieldSemantics.Normal);
        definition.Map(context.VertexType, "vUV", VertexFieldSemantics.UV);
    }

    protected override void OnRendering(ShaderDataDispatcher dispatcher, in RenderingContext context)
    {
        dispatcher.SendUniform("ma", _ambient);
        dispatcher.SendUniform("md", _diffuse);
        dispatcher.SendUniform("ms", _specular);
        dispatcher.SendUniform("shininess", _shininess);

        dispatcher.SendUniform("projection", context.Projection);
        dispatcher.SendUniform("_model", context.Model);
        dispatcher.SendUniform("view", context.View);

        var texture = _texture;
        if(texture != null) {
            dispatcher.SendUniformTexture2D("tex_sampler", texture.TextureObject, 0);
        }
        dispatcher.SendUniform("hasTexture", texture != null);

        var screen = context.Target.GetValidScreen();
        var lights = context.RenderPipeline.Lights;
        var light = lights.Length switch
        {
            > 0 => (
                Exists: true,
                Mat: lights[0].ShadowMap.LightMatrices[0],
                Pos: lights[0].Position,
                Color: lights[0].Color,
                ShadowMap: lights[0].ShadowMap.LightDepthTexture
            ),
            _ => default,
        };
        dispatcher.SendUniform("_lightMat", light.Mat);
        dispatcher.SendUniform("_lPos", light.Pos);
        dispatcher.SendUniform("_lColor", light.Color);
        dispatcher.SendUniformTexture2DArray("_shadowMap", light.ShadowMap, 1);
        dispatcher.SendUniform("_lightExists", light.Exists);
    }

    protected override ShaderSource GetShaderSource(in ShaderGetterContext context)
    {
        return new()
        {
            OnlyContainsConstLiteralUtf8 = true,
            VertexShader =
"""
#version 410
in vec3 vPos;
in vec3 vNormal;
in vec2 vUV;
out vec3 _vout_pos;
out vec3 _vout_normal;
out vec2 _vout_uv;
out vec3 _vout_shadowMapNDC;

uniform mat4 _model;
uniform mat4 view;
uniform mat4 projection;
uniform mat4 _lightMat;

void main()
{
    _vout_pos = vPos;
    _vout_normal = vNormal;
    _vout_uv = vUV;
    vec4 posLightSpace = _lightMat * _model * vec4(vPos, 1.0);
    _vout_shadowMapNDC = posLightSpace.xyz / posLightSpace.w;
    gl_Position = projection * view * _model * vec4(vPos, 1.0);
}
"""u8,
            FragmentShader =
"""
#version 410
in vec3 _vout_pos;
in vec3 _vout_normal;
in vec2 _vout_uv;
in vec3 _vout_shadowMapNDC;
out vec4 fragColor;
uniform mat4 _model;
uniform mat4 view;
uniform mat4 projection;
uniform vec3 ma;
uniform vec3 md;
uniform vec3 ms;
uniform float shininess;

uniform sampler2D tex_sampler;
uniform bool hasTexture;

uniform sampler2DArray _shadowMap;
uniform bool _lightExists;
uniform vec4 _lPos;
uniform vec4 _lColor;

float CalcShadow(vec3 shadowMapNDC, sampler2DArray shadowMap)
{
    const float bias = 0.001;
    vec3 range = 1.0 - step(vec3(1.0), abs(shadowMapNDC));
    float filter = range.x * range.y * range.z;
    vec3 shadowMapUV = shadowMapNDC * 0.5 + 0.5;
    float d = textureLod(shadowMap, vec3(shadowMapUV.xy, 0), 0).x;
    float shadow = 1.0 - step(shadowMapUV.z - bias, d);
    return shadow * filter;
}

void main()
{
    mat4 modelView = view * _model;
    vec3 posView = (modelView * vec4(_vout_pos, 1.0)).xyz;                  // vertex pos in eye space
    vec3 normalView = transpose(inverse(mat3(modelView))) * _vout_normal;   // normal in eye space
    if(_lightExists) {
        vec4 lPosView = view * _lPos;
        vec3 L = (lPosView.w == 0.0) ? normalize(lPosView.xyz) : normalize(lPosView.xyz / lPosView.w - posView);
        vec3 N = normalize(normalView);
        vec3 R = reflect(-L, N);
        vec3 V = normalize(-posView);
        vec3 l = _lColor.rgb;
        vec3 la = l * 0.6;
        vec3 ld = l * 0.8;
        vec3 ls = l * 0.2;
        vec3 ambient = la * ma;
        vec3 diffuse = ld * md * dot(N, L);
        vec3 specular = ls * ms * max(pow(max(0.0, dot(R, V)), shininess), 0.0);
        float shadow = CalcShadow(_vout_shadowMapNDC, _shadowMap);
        vec3 lightColor = ambient + (diffuse + specular) * (1.0 - shadow);
        fragColor = hasTexture ? vec4(lightColor, 1.0) * texture(tex_sampler, _vout_uv)
                           : vec4(lightColor, 1.0);
    }
    else {
        fragColor = vec4(0, 0, 0, 1);
    }
}
"""u8,
        };
    }
}
