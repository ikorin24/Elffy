#nullable enable
using Elffy.Components;
using Elffy.Components.Implementation;
using Elffy.Imaging;
using Elffy.Shading;
using System;
using Elffy.Graphics.OpenGL;

namespace Elffy.Serialization.Gltf;

internal sealed class GlbShader : RenderingShader
{
    private TextureCore _baseColorTex;
    private TextureCore _normalTex;

    protected override string VertexShaderSource => VertexShader;

    protected override string FragmentShaderSource => FragmentShader;

    public GlbShader()
    {
    }

    public void SetBaseColorTexture(ReadOnlyImageRef image, TextureConfig config)
    {
        _baseColorTex = new TextureCore(config);
        _baseColorTex.Load(image);
    }

    public void SetNormalTexture(ReadOnlyImageRef image, TextureConfig config)
    {
        _normalTex = new TextureCore(config);
        _normalTex.Load(image);
    }

    protected override void DefineLocation(VertexDefinition definition, Renderable target, Type vertexType)
    {
        definition.Map(vertexType, "_pos", VertexSpecialField.Position);
        definition.Map(vertexType, "_uv", VertexSpecialField.UV);
        definition.Map(vertexType, "_normal", VertexSpecialField.Normal);
    }

    protected override void OnRendering(ShaderDataDispatcher dispatcher, Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
    {
        dispatcher.SendUniform("_mvp", projection * view * model);
        dispatcher.SendUniformTexture2D("_baseColorTex", _baseColorTex.Texture, TextureUnitNumber.Unit0);
        if(_normalTex.IsEmpty == false) {
            dispatcher.SendUniformTexture2D("_normalTex", _normalTex.Texture, TextureUnitNumber.Unit1);
        }
        dispatcher.SendUniform("_hasNormalTex", !_normalTex.IsEmpty);
    }

    protected override void OnProgramDisposed()
    {
        _baseColorTex.Dispose();
        _normalTex.Dispose();
    }

    private const string VertexShader =
@"#version 410
in vec3 _pos;
in vec2 _uv;
in vec3 _normal;
out vec2 _vUV;
out vec3 _vNormal;
uniform mat4 _mvp;
void main()
{
    _vUV = _uv;
    gl_Position = _mvp * vec4(_pos, 1.0);
}
";
    private const string FragmentShader =
@"#version 410
in vec2 _vUV;
in vec3 _vNormal;
uniform sampler2D _baseColorTex;
uniform sampler2D _normalTex;
uniform bool _hasNormalTex;
out vec4 _fragColor;
void main()
{
    vec3 result;
    vec3 baseColor = texture(_baseColorTex, _vUV).rgb;
    vec3 n = _hasNormalTex ? texture(_normalTex, _vUV).rgb * 2 - vec3(1, 1, 1) : _vNormal;

    //result = n;
    result = texture(_normalTex, _vUV).rgb * 2.0 - vec3(1.0, 1.0, 1.0);
    result = baseColor;
    _fragColor = vec4(result, 1);
}
";
}
