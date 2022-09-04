#nullable enable
using Elffy.Components;
using Elffy.Components.Implementation;
using Elffy.Imaging;
using Elffy.Shading;
using Elffy.Graphics.OpenGL;
using System;
using System.Diagnostics.CodeAnalysis;
using Elffy.Features;

namespace Elffy.Serialization.Gltf;

internal sealed class GlbShader : SingleTargetRenderingShader
{
    private bool _disposed = false;
    private TextureCore _baseColorTex;
    private TextureCore _normalTex;

    private bool _isSafetyRegistered;
    private ContextAssociatedMemorySafety.SafetyKey _safety;

    public GlbShader()
    {
    }

    ~GlbShader()
    {
        if(_isSafetyRegistered) {
            ContextAssociatedMemorySafety.OnFinalized(_safety);
        }
    }

    public void SetBaseColorTexture(ReadOnlyImageRef image, TextureConfig config)
    {
        if(_disposed) {
            ThrowAlreadyDisposed();
        }
        var screen = Engine.GetValidCurrentContext();
        _baseColorTex = new TextureCore(config);
        _baseColorTex.Load(image);

        if(_isSafetyRegistered == false) {
            _safety = ContextAssociatedMemorySafety.RegisterNonDisposable(this, static self => self.DisposeUnmanagedData(), screen);
            _isSafetyRegistered = true;
        }
    }

    public void SetNormalTexture(ReadOnlyImageRef image, TextureConfig config)
    {
        if(_disposed) {
            ThrowAlreadyDisposed();
        }
        var screen = Engine.GetValidCurrentContext();
        _normalTex = new TextureCore(config);
        _normalTex.Load(image);

        if(_isSafetyRegistered == false) {
            _safety = ContextAssociatedMemorySafety.RegisterNonDisposable(this, static self => self.DisposeUnmanagedData(), screen);
            _isSafetyRegistered = true;
        }
    }

    [DoesNotReturn]
    private static void ThrowAlreadyDisposed() => throw new ObjectDisposedException(nameof(GlbShader), $"The instance is already disposed.");

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
        GC.SuppressFinalize(this);
        DisposeUnmanagedData();
    }

    private void DisposeUnmanagedData()
    {
        _baseColorTex.Dispose();
        _normalTex.Dispose();
        _disposed = true;
    }

    protected override void OnTargetAttached(Renderable target) { }

    protected override void OnTargetDetached(Renderable detachedTarget) { }

    protected override ShaderSource GetShaderSource(Renderable target, WorldLayer layer) => GetForwardShader();

    private static ShaderSource GetForwardShader() => new()
    {
        VertexShader = @"#version 410
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
",
        FragmentShader =
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
",
    };
}
