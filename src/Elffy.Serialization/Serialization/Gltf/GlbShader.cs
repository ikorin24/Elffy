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

        dispatcher.SendUniform("_hasBaseColorTex", TryGetTexture(_baseColor, out var baseColor));
        dispatcher.SendUniformTexture2D("_baseColorTex", baseColor, TextureUnitNumber.Unit0);

        dispatcher.SendUniform("_hasNormalTex", TryGetTexture(_normal, out var normal));
        dispatcher.SendUniformTexture2D("_normalTex", normal, TextureUnitNumber.Unit1);

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

    private static bool TryGetTexture(Texture? texture, out TextureObject to)
    {
        if(texture == null) {
            to = TextureObject.Empty;
            return false;
        }
        to = texture.TextureObject;
        return to.IsEmpty == false;
    }

    protected override void OnProgramDisposed()
    {
        _baseColor?.Dispose();
        _baseColor = null;
        _normal?.Dispose();
        _normal = null;
    }

    protected override void OnTargetAttached(Renderable target) { }

    protected override void OnTargetDetached(Renderable detachedTarget) { }

    protected override ShaderSource GetShaderSource(Renderable target, WorldLayer layer) => GetForwardShader();

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
uniform bool _hasBaseColorTex;
uniform bool _hasNormalTex;
out vec4 _fragColor;
const vec3 NoColor = vec3(1, 0, 1);


uniform mat4 _model;
uniform mat4 _view;

void main()
{
    vec3 baseColor = _hasBaseColorTex ? texture(_baseColorTex, _v2f.uv).rgb : NoColor;
    vec3 normalTan = texture(_normalTex, _v2f.uv).rgb * 2 - vec3(1, 1, 1);


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
}
",
    };
}
