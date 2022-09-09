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
            null => (default, Color4.Black),
            _ => (light.Position, light.Color),
        };
        dispatcher.SendUniform("_lpos", lpos);
        dispatcher.SendUniform("_lcolor", lcolor);
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
    vec3 pos;
    vec2 uv;
    vec3 normal;
    mat3 tbn;   // camera space -> tangent space
    vec3 ldirTan;   // light dir in tangent space
} _v2f;
uniform mat4 _model;
uniform mat4 _view;
uniform mat4 _projection;
uniform vec4 _lpos;
void main()
{
    _v2f.pos = _pos;
    _v2f.uv = _uv;
    _v2f.normal = _normal;
    mat4 modelView = _view * _model;
    vec3 bitangent = cross(_normal, _tangent);
    mat3 mvMat3 = mat3(modelView);
    _v2f.tbn = transpose(mat3(mvMat3 * _tangent, mvMat3 * bitangent, mvMat3 * _normal));

    vec4 vposCam = modelView * vec4(_pos, 1.0);
    gl_Position = _projection * vposCam;

    if(_lpos.w <= 0.001) {
        vec3 ldirCam = mat3(_view) * -normalize(_lpos.xyz);
        _v2f.ldirTan = _v2f.tbn * ldirCam;
    }
    else {
        vec4 lposCam = _view * vec4((_lpos.xyz / _lpos.w), 1.0);
        vec3 ldirCam = normalize(vposCam.xyz / vposCam.w - lposCam.xyz / lposCam.w);
        _v2f.ldirTan = _v2f.tbn * ldirCam;
    }
}
",
        FragmentShader =
@"#version 410
in V2f
{
    vec3 pos;
    vec2 uv;
    vec3 normal;
    mat3 tbn;
    vec3 ldirTan;
} _v2f;
uniform vec4 _lcolor;
uniform sampler2D _baseColorTex;
uniform sampler2D _normalTex;
uniform sampler2D _metallicRoughnessTex;
out vec4 _fragColor;
uniform mat4 _model;
uniform mat4 _view;

void main()
{
    vec3 baseColor = texture(_baseColorTex, _v2f.uv).rgb;
    vec3 normalTan = texture(_normalTex, _v2f.uv).rgb * 2 - vec3(1, 1, 1);
    vec2 metallicRoughness = texture(_metallicRoughnessTex, _v2f.uv).rg;

    const float afactor = 0.8;
    const float dfactor = 0.35;
    const float sfactor = 0.2;
    const float shininess = 1;
    float dotNL = max(0, dot(normalTan, -_v2f.ldirTan));
    vec3 ambient = afactor * baseColor;
    vec3 diffuse = dotNL * dfactor * baseColor;
    vec3 rTan = reflect(_v2f.ldirTan, normalTan);

    vec3 V = _v2f.tbn * -(_view * _model * vec4(_v2f.pos, 1)).xyz;
    vec3 specular = max(pow(max(0.0, dot(rTan, V)), shininess), 0.0) * sfactor * baseColor;
    vec3 result = (ambient + diffuse + specular) * _lcolor.rgb;
    _fragColor = vec4(result, 1);

    //_fragColor = vec4(0, metallicRoughness.g, 0, 1);
    //_fragColor = vec4(metallicRoughness.r, 0, 0, 1);
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
    vec3 pos;
    vec2 uv;
    vec3 normal;
    mat3 tbn;   // camera space -> tangent space
    vec3 ldirTan;   // light dir in tangent space
} _v2f;
uniform mat4 _model;
uniform mat4 _view;
uniform mat4 _projection;
uniform vec4 _lpos;
void main()
{
    _v2f.pos = _pos;
    _v2f.uv = _uv;
    _v2f.normal = _normal;
    mat4 modelView = _view * _model;
    vec3 bitangent = cross(_normal, _tangent);
    mat3 mvMat3 = mat3(modelView);
    _v2f.tbn = transpose(mat3(mvMat3 * _tangent, mvMat3 * bitangent, mvMat3 * _normal));

    vec4 vposCam = modelView * vec4(_pos, 1.0);
    gl_Position = _projection * vposCam;

    if(_lpos.w <= 0.001) {
        vec3 ldirCam = mat3(_view) * -normalize(_lpos.xyz);
        _v2f.ldirTan = _v2f.tbn * ldirCam;
    }
    else {
        vec4 lposCam = _view * vec4((_lpos.xyz / _lpos.w), 1.0);
        vec3 ldirCam = normalize(vposCam.xyz / vposCam.w - lposCam.xyz / lposCam.w);
        _v2f.ldirTan = _v2f.tbn * ldirCam;
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
    vec3 pos;
    vec2 uv;
    vec3 normal;
    mat3 tbn;   // camera space -> tangent space
    vec3 ldirTan;   // light dir in tangent space
} _v2f;

uniform mat4 _model;
uniform mat4 _view;
uniform sampler2D _baseColorTex;
uniform sampler2D _normalTex;
uniform sampler2D _metallicRoughnessTex;
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
    vec2 metallicRoughness = texture(_metallicRoughnessTex, _v2f.uv).rg;
    vec3 normalTan = texture(_normalTex, _v2f.uv).rgb * 2 - vec3(1, 1, 1);
    vec3 normalWorld = normalize(transpose(mat3(_view)) * transpose(_v2f.tbn) * normalTan);

    _mrt0 = vec4(ToVec3(_model * vec4(_v2f.pos, 1.0)).xyz, 1.0);
    _mrt1 = vec4(normalWorld, metallicRoughness.y);
    _mrt2 = vec4(texture(_baseColorTex, _v2f.uv).rgb, metallicRoughness.x);
    _mrt3 = vec4(0, 0, 0, 0);
    _mrt4 = vec4(0, 0, 0, 0);
}",
    };
}
