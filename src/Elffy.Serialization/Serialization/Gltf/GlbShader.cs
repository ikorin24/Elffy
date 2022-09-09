#nullable enable
using Elffy.Imaging;
using Elffy.Shading;
using Elffy.Graphics.OpenGL;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Serialization.Gltf;

internal sealed class GlbShader : SingleTargetRenderingShader
{
    private Texture? _baseColor;
    private Texture? _normal;
    private Texture? _metallicRoughness;

    private Color4 _baseColorFactor;
    private float _metallicFactor;
    private float _roughnessFactor;

    public Color4 BaseColorFactor { get => _baseColorFactor; set => _baseColorFactor = value; }
    public float MetallicFactor { get => _metallicFactor; set => _metallicFactor = value; }
    public float RoughnessFactor { get => _roughnessFactor; set => _roughnessFactor = value; }

    public GlbShader()
    {
    }

    public void SetBaseColorTexture(ReadOnlyImageRef image, TextureConfig config)
    {
        var texture = new Texture(config);
        texture.Load(image);
        _baseColor?.Dispose();
        _baseColor = texture;
    }

    public void SetNormalTexture(ReadOnlyImageRef image, TextureConfig config)
    {
        var texture = new Texture(config);
        texture.Load(image);
        _normal?.Dispose();
        _normal = texture;
    }

    public void SetMetallicRoughnessTexture(ReadOnlyImageRef image, TextureConfig config)
    {
        var texture = new Texture(config);
        texture.Load(image);
        _metallicRoughness?.Dispose();
        _metallicRoughness = texture;
    }

    [DoesNotReturn]
    private static void ThrowAlreadyDisposed() => throw new ObjectDisposedException(nameof(GlbShader), $"The instance is already disposed.");

    protected override void DefineLocation(VertexDefinition definition, Renderable target, Type vertexType)
    {
        definition.Map(vertexType, "_pos", VertexSpecialField.Position);
        definition.Map(vertexType, "_uv", VertexSpecialField.UV);
        definition.Map(vertexType, "_normal", VertexSpecialField.Normal);
        definition.Map(vertexType, "_tangent", VertexSpecialField.Tangent);
    }

    protected override void OnRendering(ShaderDataDispatcher dispatcher, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
    {
        dispatcher.SendUniform("_model", model);
        dispatcher.SendUniform("_view", view);
        dispatcher.SendUniform("_projection", projection);

        var baseColorTex = _baseColor?.TextureObject ?? TextureObject.Empty;
        var normalTex = _normal?.TextureObject ?? TextureObject.Empty;
        var metallicRoughnessTex = _metallicRoughness?.TextureObject ?? TextureObject.Empty;
        dispatcher.SendUniformTexture2D("_baseColorTex", baseColorTex, TextureUnitNumber.Unit0);
        dispatcher.SendUniformTexture2D("_normalTex", normalTex, TextureUnitNumber.Unit1);
        dispatcher.SendUniformTexture2D("_metallicRoughnessTex", metallicRoughnessTex, TextureUnitNumber.Unit2);
        var screen = target.GetValidScreen();
        var lights = screen.Lights.GetLights();
        var light = lights.FirstOrDefault();
        var (lpos, lcolor) = light switch
        {
            not null => (light.Position, light.Color),
            _ => (default, Color4.Black),
        };
        dispatcher.SendUniform("_lpos", lpos);
        dispatcher.SendUniform("_lcolor", lcolor);
        dispatcher.SendUniform("_baseColorFactor", _baseColorFactor);
        dispatcher.SendUniform("_metallicFactor", _metallicFactor);
        dispatcher.SendUniform("_roughnessFactor", _roughnessFactor);
    }

    protected override void OnProgramDisposed()
    {
        _baseColor?.Dispose();
        _baseColor = null;
        _normal?.Dispose();
        _normal = null;
        _metallicRoughness?.Dispose();
        _metallicRoughness = null;
    }

    protected override void OnTargetAttached(Renderable target) { }

    protected override void OnTargetDetached(Renderable detachedTarget) { }

    protected override ShaderSource GetShaderSource(Renderable target, WorldLayer layer)
    {
        return layer switch
        {
            DeferredRenderingLayer => GetDeferredShader(),
            _ => GetForwardShader(),
        };
    }

    private static ShaderSource GetForwardShader() => new()
    {
        VertexShader =
@"#version 410
in vec3 _pos;
in vec2 _uv;
in vec3 _normal;
in vec3 _tangent;
out V2f
{
    vec2 uv;
    vec3 normal;
    mat3 tbn;       // camera space -> tangent space
    vec3 ldirTan;   // light dir in tangent space
    vec3 cdirTan;   // camera dir in tangent space
} _v2f;
uniform mat4 _model;
uniform mat4 _view;
uniform mat4 _projection;
uniform vec4 _lpos;
void main()
{
    _v2f.uv = _uv;
    _v2f.normal = _normal;
    mat4 modelView = _view * _model;
    vec3 bitangent = cross(_normal, _tangent);
    mat3 mvMat3 = mat3(modelView);
    _v2f.tbn = transpose(mat3(mvMat3 * _tangent, mvMat3 * bitangent, mvMat3 * _normal));

    vec4 vposCam = modelView * vec4(_pos, 1.0);
    vec3 vposCam3 = vposCam.xyz / vposCam.w;

    gl_Position = _projection * vposCam;

    if(_lpos.w <= 0.001) {
        _v2f.ldirTan = normalize(_v2f.tbn * mat3(_view) * -_lpos.xyz);
    }
    else {
        vec4 lposCam = _view * vec4((_lpos.xyz / _lpos.w), 1.0);
        _v2f.ldirTan = normalize(_v2f.tbn * (vposCam3 - lposCam.xyz / lposCam.w));
    }
    _v2f.cdirTan = normalize(_v2f.tbn * -vposCam3);
}
",
        FragmentShader =
@"#version 410
#define m_float mediump float
#define m_vec2  mediump vec2
#define m_vec3  mediump vec3
#define m_vec4  mediump vec4
#define h_float highp float
#define h_vec2 highp vec2
#define h_vec3 highp vec3
#define h_vec4 highp vec4
in V2f
{
    vec2 uv;
    vec3 normal;
    mat3 tbn;
    vec3 ldirTan;
    vec3 cdirTan;
} _v2f;

uniform vec4 _lcolor;
uniform sampler2D _baseColorTex;
uniform sampler2D _normalTex;
uniform sampler2D _metallicRoughnessTex;
uniform vec4 _baseColorFactor;
uniform float _metallicFactor;
uniform float _roughnessFactor;
uniform mat4 _model;
uniform mat4 _view;

out vec4 _fragColor;

const float INV_PI = 1.0 / 3.1415926;
const float DielectricF0 = 0.04;

float Fd_Burley(float dot_nv, float dot_nl, float dot_lh, float roughness)
{
    float fd90 = 0.5 + 2.0 * dot_lh * dot_lh * roughness;
    float p = 1.0 - dot_nl;
    float q = 1.0 - dot_nv;
    float p2 = p * p;
    float q2 = q * q;
    float p5 = p2 * p2 * p;
    float q5 = q2 * q2 * q;
    float lightScatter = 1.0 + (fd90 - 1.0) * p5;
    float viewScatter = 1.0 + (fd90 - 1.0) * q5;
    return lightScatter * viewScatter * INV_PI;
}

float V_SmithGGXCorrelated(float dot_nl, float dot_nv, float alpha)    // Height-Correlated Smith
{
    // For optimization, we will approximate the following expression.
    // (This approximation is not mathematically correct, but it works fine.)

    // float a2 = alpha * alpha;
    // float lambdaV = dot_nl * sqrt((-dot_nv * a2 + dot_nv) * dot_nv + a2);
    // float lambdaL = dot_nv * sqrt((-dot_nl * a2 + dot_nl) * dot_nl + a2);

    float beta = 1.0 - alpha;
    float lambdaV = dot_nl * (dot_nv * beta + alpha);
    float lambdaL = dot_nv * (dot_nl * beta + alpha);

    return 0.5 / (lambdaV + lambdaL + 0.0001);
}

m_float D_GGX(m_vec3 n, m_vec3 h, m_float dot_nh, m_float roughness)     // Trowbridge-Reitz
{
    m_float p = roughness * dot_nh;
    m_vec3 cross_nh = cross(n, h);
    m_float q = roughness / (dot(cross_nh, cross_nh) + p * p);
    return min(q * q * INV_PI, 16300.0);        // 16300.0 is about 2^14, safe max of mediump float
}

m_vec3 F_Schlick(m_vec3 f0, m_float u)
{
    vec3 f90 = vec3(1.0, 1.0, 1.0);
    m_float x = 1.0 - u;
    m_float x2 = x * x;
    m_float x5 = x2 * x2 * x;
    return f0 + (f90 - f0) * x5;
}

void main()
{
    vec3 baseColor = texture(_baseColorTex, _v2f.uv).rgb * _baseColorFactor.rgb;
    vec3 normalTan = texture(_normalTex, _v2f.uv).rgb * 2 - vec3(1, 1, 1);
    vec2 metallicRoughness = texture(_metallicRoughnessTex, _v2f.uv).rg;

    vec3 v = -_v2f.cdirTan;
    vec3 n = normalTan;
    vec3 l = -_v2f.ldirTan;
    vec3 h = normalize(-v + l);
    float metallic = metallicRoughness.r * _metallicFactor;
    float roughness = metallicRoughness.g * _roughnessFactor;
    float reflectivity = mix(DielectricF0, 1.0, metallic);
    vec3 f0 = mix(vec3(DielectricF0, DielectricF0, DielectricF0), baseColor, metallic);
    vec3 lColor = _lcolor.xyz;
    float dot_nl = max(0.0, dot(n, l));
    float dot_nh = max(0.0, dot(n, h));
    float dot_lh = max(0.0, dot(l, h));
    float dot_nv = abs(dot(n, v));
    float diffuseTerm = Fd_Burley(dot_nv, dot_nl, dot_lh, roughness) * dot_nl;
    vec3 diffuse = (1.0 - reflectivity) * diffuseTerm * lColor * baseColor;

    float alpha = roughness * roughness;
    float V = V_SmithGGXCorrelated(dot_nl, dot_nv, alpha);
    float D = D_GGX(n, h, dot_nh, roughness);
    vec3 F = F_Schlick(f0, dot_lh);
    vec3 specular = max(V * D * F * dot_nl * lColor, vec3(0.0, 0.0, 0.0));

    _fragColor = vec4(diffuse + specular, 1.0);
}
",
    };

    private static ShaderSource GetDeferredShader() => new()
    {
        VertexShader =
@"#version 410
in vec3 _pos;
in vec2 _uv;
in vec3 _normal;
in vec3 _tangent;
out V2f
{
    vec3 vposWorld;
    vec2 uv;
    vec3 normal;
    mat3 tbn;       // camera space -> tangent space
    vec3 ldirTan;   // light dir in tangent space
} _v2f;
uniform mat4 _model;
uniform mat4 _view;
uniform mat4 _projection;
uniform vec4 _lpos;
void main()
{
    vec4 vposWorld4 = _model * vec4(_pos, 1.0);
    _v2f.vposWorld = vposWorld4.xyz / vposWorld4.w;
    _v2f.uv = _uv;
    _v2f.normal = _normal;
    mat4 modelView = _view * _model;
    vec3 bitangent = cross(_normal, _tangent);
    mat3 mvMat3 = mat3(modelView);
    _v2f.tbn = transpose(mat3(mvMat3 * _tangent, mvMat3 * bitangent, mvMat3 * _normal));

    vec4 vposCam = modelView * vec4(_pos, 1.0);
    gl_Position = _projection * vposCam;

    if(_lpos.w <= 0.001) {
        _v2f.ldirTan = normalize(_v2f.tbn * mat3(_view) * -_lpos.xyz);
    }
    else {
        vec4 lposCam = _view * vec4((_lpos.xyz / _lpos.w), 1.0);
        _v2f.ldirTan = normalize(_v2f.tbn * (vposCam.xyz / vposCam.w - lposCam.xyz / lposCam.w));
    }
}
",
        // index  | R           | G            | B           | A         |
        // ----
        // mrt[0] | pos.x       | pos.y        | pos.z       | 1         |
        // mrt[1] | normal.x    | normal.y     | normal.z    | roughness |
        // mrt[2] | baseColor.r | baseColor.g  | baseColor.b | metallic  |
        // mrt[3] | 0           | 0            | 0           | 0         |
        // mrt[4] | 0           | 0            | 0           | 0         |
        FragmentShader =
@"#version 410
in V2f
{
    vec3 vposWorld;
    vec2 uv;
    vec3 normal;
    mat3 tbn;       // camera space -> tangent space
    vec3 ldirTan;   // light dir in tangent space
} _v2f;

uniform mat4 _model;
uniform mat4 _view;
uniform sampler2D _baseColorTex;
uniform sampler2D _normalTex;
uniform sampler2D _metallicRoughnessTex;
uniform vec4 _baseColorFactor;
uniform float _metallicFactor;
uniform float _roughnessFactor;
layout (location = 0) out vec4 _mrt0;
layout (location = 1) out vec4 _mrt1;
layout (location = 2) out vec4 _mrt2;
layout (location = 3) out vec4 _mrt3;
layout (location = 4) out vec4 _mrt4;

vec3 ToVec3(vec4 v)
{
    return v.xyz / v.w;
}

void main()
{
    vec3 baseColor = _baseColorFactor.rgb * texture(_baseColorTex, _v2f.uv).rgb;
    vec2 metallicRoughness = texture(_metallicRoughnessTex, _v2f.uv).rg;
    vec3 normalTan = texture(_normalTex, _v2f.uv).rgb * 2 - vec3(1, 1, 1);
    vec3 normalWorld = normalize(transpose(mat3(_view)) * transpose(_v2f.tbn) * normalTan);
    float metallic = _metallicFactor * metallicRoughness.x;
    float roughness = _roughnessFactor * metallicRoughness.y;

    _mrt0 = vec4(_v2f.vposWorld, 1.0);
    _mrt1 = vec4(normalWorld, roughness);
    _mrt2 = vec4(baseColor, metallic);
    _mrt3 = vec4(0, 0, 0, 0);
    _mrt4 = vec4(0, 0, 0, 0);
}",
    };
}
