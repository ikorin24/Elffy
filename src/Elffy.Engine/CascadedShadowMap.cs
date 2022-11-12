#nullable enable
using Elffy.Effective;
using Elffy.Features;
using Elffy.Graphics.OpenGL;
using System;
using System.Runtime.CompilerServices;

namespace Elffy;

public sealed class CascadedShadowMap
{
    private readonly DirectLight _light;

    private CascadedShadowMapData _cascades;
    private MappedFloatDataTextureCore<Matrix4> _lightMatrices;
    private bool _isInitialized;

    public DirectLight Light => _light;
    public int CascadeCount => _cascades.CascadeCount;
    public Vector2i Size => _cascades.Size;

    /// <summary>Depth texture from a light. (Texture2DArray)</summary>
    public TextureObject LightDepthTexture => _cascades.DepthTexture;
    public ReadOnlySpan<Matrix4> LightMatrices => _lightMatrices.AsSpan();
    /// <summary>Light matrix in Texture1D</summary>
    public TextureObject LightMatricesDataTexture => _lightMatrices.TextureObject;
    public FBO ShadowMappingFbo => _cascades.Fbo;

    internal CascadedShadowMap(DirectLight light)
    {
        _light = light;
        _cascades = new CascadedShadowMapData();
    }

    internal void Initialize(DirectLightConfig config)
    {
        if(_isInitialized) {
            throw new InvalidOperationException("Cannot initialize a shadow map twice.");
        }
        config.ValidateArg();
        _cascades.Initialize(config.ShadowMapSize, config.ShadowMapSize, config.CascadeCount);
        _lightMatrices.LoadZero(config.CascadeCount);
        _isInitialized = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void UpdateLightMatrices(ReadOnlySpan<Matrix4> lightMatrices)
    {
        if(lightMatrices.Length != CascadeCount) {
            throw new ArgumentException(nameof(lightMatrices));
        }
        _lightMatrices.Update(lightMatrices.MarshalCast<Matrix4, Color4>(), 0);
    }

    internal void Release()
    {
        _cascades.Release();
        _lightMatrices.Dispose();
    }
}
