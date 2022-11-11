#nullable enable
using System;
using Elffy.Graphics.OpenGL;
using OpenTK.Graphics.OpenGL4;

namespace Elffy;

internal struct CascadedShadowMapData
{
    private FBO _fbo;
    private TextureObject _depth;
    private Vector2i _size;
    private int _cascadeCount;

    public static CascadedShadowMapData Empty => default;

    public readonly bool IsEmpty => _depth.IsEmpty;

    public readonly Vector2i Size => _size;

    public readonly int CascadeCount => _cascadeCount;

    /// <summary>Depth texture from a light. (Texture2DArray)</summary>
    public readonly TextureObject DepthTexture => _depth;

    public readonly FBO Fbo => _fbo;

    internal void Initialize(int width, int height, int cascadeCount)
    {
        if(width <= 0) { throw new ArgumentOutOfRangeException(nameof(width)); }
        if(height <= 0) { throw new ArgumentOutOfRangeException(nameof(width)); }
        if(cascadeCount <= 0) { throw new ArgumentOutOfRangeException(nameof(cascadeCount)); }
        (_, _fbo, _depth, _size, _cascadeCount) = Create(width, height, cascadeCount);
    }

    private static (IHostScreen Screen, FBO Fbo, TextureObject DepthTex, Vector2i Size, int CascadeCount) Create(int width, int height, int cascadeCount)
    {
        var screen = Engine.GetValidCurrentContext();
        var size = new Vector2i(width, height);
        var depth = TextureObject.Empty;
        var fbo = FBO.Empty;
        try {
            depth = TextureObject.Create();
            TextureObject.Bind2DArray(depth);
            TextureObject.DepthImage2DArrayUninitialized(size, cascadeCount);
            TextureObject.Parameter2DArrayMinFilter(TextureShrinkMode.NearestNeighbor, TextureMipmapMode.None);
            TextureObject.Parameter2DArrayMagFilter(TextureExpansionMode.NearestNeighbor);
            TextureObject.Parameter2DArrayWrapS(TextureWrap.ClampToBorder);
            TextureObject.Parameter2DArrayWrapT(TextureWrap.ClampToBorder);
            fbo = FBO.Create();
            FBO.Bind(fbo, FBO.Target.FrameBuffer);
            FBO.SetTextureDepthAttachment(depth);
            GL.DrawBuffer(DrawBufferMode.None);
            GL.ReadBuffer(ReadBufferMode.None);
            FBO.ThrowIfInvalidStatus();
            FBO.Unbind(FBO.Target.FrameBuffer);
            return (Screen: screen, Fbo: fbo, DepthTex: depth, Size: size, CascadeCount: cascadeCount);
        }
        catch {
            TextureObject.Delete(ref depth);
            FBO.Delete(ref fbo);
            throw;
        }
    }

    internal void Release()
    {
        FBO.Delete(ref _fbo);
        TextureObject.Delete(ref _depth);
        _size = Vector2i.Zero;
    }
}
