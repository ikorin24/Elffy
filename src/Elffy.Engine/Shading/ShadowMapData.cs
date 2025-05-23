﻿#nullable enable
using System;
using Elffy.Graphics.OpenGL;
using OpenTK.Graphics.OpenGL4;

namespace Elffy.Shading;

internal struct ShadowMapData
{
    private FBO _fbo;
    private TextureObject _depth;
    private Vector2i _size;

    public static ShadowMapData Empty => default;

    public readonly bool IsEmpty => _depth.IsEmpty;

    public readonly Vector2i Size => _size;

    public readonly TextureObject DepthTexture => _depth;

    public readonly FBO Fbo => _fbo;

    internal void Initialize(int width, int height)
    {
        if(width <= 0) { throw new ArgumentOutOfRangeException(nameof(width)); }
        if(height <= 0) { throw new ArgumentOutOfRangeException(nameof(width)); }
        (_, _fbo, _depth, _size) = Create(width, height);
    }

    private static (IHostScreen Screen, FBO Fbo, TextureObject DepthTex, Vector2i Size) Create(int width, int height)
    {
        var screen = Engine.GetValidCurrentContext();
        var size = new Vector2i(width, height);
        var depth = TextureObject.Empty;
        var fbo = FBO.Empty;
        try {
            depth = TextureObject.Create();
            TextureObject.Bind2D(depth);
            TextureObject.DepthImage2DUninitialized(size);
            TextureObject.Parameter2DMinFilter(TextureShrinkMode.NearestNeighbor, TextureMipmapMode.None);
            TextureObject.Parameter2DMagFilter(TextureExpansionMode.NearestNeighbor);
            TextureObject.Parameter2DWrapS(TextureWrap.ClampToBorder);
            TextureObject.Parameter2DWrapT(TextureWrap.ClampToBorder);
            fbo = FBO.Create();
            FBO.Bind(fbo, FBO.Target.FrameBuffer);
            FBO.SetTextureDepthAttachment(depth);
            GL.DrawBuffer(DrawBufferMode.None);
            GL.ReadBuffer(ReadBufferMode.None);
            FBO.ThrowIfInvalidStatus();
            FBO.Unbind(FBO.Target.FrameBuffer);
            return (Screen: screen, Fbo: fbo, DepthTex: depth, Size: size);
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
