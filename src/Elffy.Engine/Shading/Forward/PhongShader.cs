#nullable enable
using Elffy.Graphics.OpenGL;
using System;

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
        dispatcher.SendUniform("view", context.View);
        dispatcher.SendUniform("modelView", context.View * context.Model);

        var texture = _texture;
        if(texture != null) {
            dispatcher.SendUniformTexture2D("tex_sampler", texture.TextureObject, TextureUnitNumber.Unit0);
        }
        dispatcher.SendUniform("hasTexture", texture != null);

        var screen = context.Target.GetValidScreen();
        var lights = screen.Lights;
        dispatcher.SendUniform("lightCount", lights.LightCount);
        dispatcher.SendUniformTexture1D("lColorSampler", lights.ColorTexture, TextureUnitNumber.Unit1);
        dispatcher.SendUniformTexture1D("lPosSampler", lights.PositionTexture, TextureUnitNumber.Unit2);

        bool hasShadowMap;

        var light = lights.GetLights().FirstOrDefault();
        if(light != null) {
            dispatcher.SendUniform("_lmvp", light.LightMatrix * context.Model);
            dispatcher.SendUniformTexture2D("_shadowMap", light.ShadowMap.DepthTexture, TextureUnitNumber.Unit3);
            hasShadowMap = true;
        }
        else {
            hasShadowMap = false;
        }
        dispatcher.SendUniform("_hasShadowMap", hasShadowMap);
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

uniform mat4 modelView;
uniform mat4 projection;
uniform mat4 _lmvp;

void main()
{
    _vout_pos = vPos;
    _vout_normal = vNormal;
    _vout_uv = vUV;
    vec4 posLightSpace = _lmvp * vec4(vPos, 1.0);
    _vout_shadowMapNDC = posLightSpace.xyz / posLightSpace.w;
    gl_Position = projection * modelView * vec4(vPos, 1.0);
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

uniform sampler2D _shadowMap;
uniform bool _hasShadowMap;

float CalcShadow(vec3 shadowMapNDC, sampler2D shadowMap)
{
    const float bias = 0.0004;
    vec3 range = 1.0 - step(vec3(1.0), abs(shadowMapNDC));
    float filter = range.x * range.y * range.z;
    vec3 shadowMapUV = shadowMapNDC * 0.5 + 0.5;
    float d = textureLod(shadowMap, shadowMapUV.xy, 0).x;
    float shadow = 1.0 - step(shadowMapUV.z - bias, d);
    return shadow * filter;
}

void main()
{
    vec3 posView = (modelView * vec4(_vout_pos, 1.0)).xyz;                  // vertex pos in eye space
    vec3 normalView = transpose(inverse(mat3(modelView))) * _vout_normal;   // normal in eye space
    vec3 lightAmbient = vec3(0, 0, 0);
    vec3 lightDiffuse = vec3(0, 0, 0);
    vec3 lightSpecular = vec3(0, 0, 0);
    for(int i = 0; i < lightCount; i++) {
        vec4 lPosView = view * texelFetch(lPosSampler, i, 0);
        vec3 L = (lPosView.w == 0.0) ? normalize(lPosView.xyz) : normalize(lPosView.xyz / lPosView.w - posView);
        vec3 N = normalize(normalView);
        vec3 R = reflect(-L, N);
        vec3 V = normalize(-posView);
        vec3 l = texelFetch(lColorSampler, i, 0).rgb;
        vec3 la = l * 0.6;
        vec3 ld = l * 0.8;
        vec3 ls = l * 0.2;
        vec3 ambient = la * ma;
        vec3 diffuse = ld * md * dot(N, L);
        vec3 specular = ls * ms * max(pow(max(0.0, dot(R, V)), shininess), 0.0);
        lightAmbient += ambient;
        lightDiffuse += diffuse;
        lightSpecular += specular;
    }
    float shadow = CalcShadow(_vout_shadowMapNDC, _shadowMap);
    vec3 lightColor = lightAmbient + (lightDiffuse + lightSpecular) * (1.0 - shadow);
    fragColor = hasTexture ? vec4(lightColor, 1.0) * texture(tex_sampler, _vout_uv)
                           : vec4(lightColor, 1.0);
}
"""u8,
        };
    }
}
