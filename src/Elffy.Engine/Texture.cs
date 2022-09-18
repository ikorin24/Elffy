#nullable enable
using System;
using Elffy.Graphics.OpenGL;
using Elffy.Imaging;
using Elffy.Features.Implementation;
using Elffy.Features;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy;

public sealed class Texture : IContextAssociatedSafety
{
    private bool _disposed;
    private ContextAssociatedSafetyImpl _safetyImpl;    // Mutable object, Don't change into readonly
    private TextureCore _textureCore;                   // Mutable object, Don't change into readonly

    public bool IsDisposed => _disposed;

    public TextureExpansionMode ExpansionMode => _textureCore.ExpansionMode;
    public TextureShrinkMode ShrinkMode => _textureCore.ShrinkMode;
    public TextureMipmapMode MipmapMode => _textureCore.MipmapMode;
    public TextureWrap WrapModeX => _textureCore.WrapModeX;
    public TextureWrap WrapModeY => _textureCore.WrapModeY;
    public TextureObject TextureObject => _textureCore.Texture;

    public bool IsEmpty => _textureCore.IsEmpty;

    public int Width => _textureCore.Size.X;

    public int Height => _textureCore.Size.Y;

    public Vector2i Size => _textureCore.Size;

    IHostScreen? IContextAssociatedSafety.AssociatedContext => _safetyImpl.AssociatedContext;

    public Texture(in TextureConfig config)
    {
        _textureCore = new TextureCore(config);
    }

    ~Texture() => Dispose(false);

    public void Load<T>(in Vector2i size, ImageBuilderDelegate<T> imageBuilder, T state)
    {
        ThrowIfDisposed();
        _textureCore.Load(state, size, imageBuilder);
        _safetyImpl.TryRegisterToCurrentContext(this, static x => x.DisposeContextAssociatedMemory());
    }

    /// <summary>Load image</summary>
    /// <param name="image">image to load</param>
    public void Load(in ReadOnlyImageRef image)
    {
        ThrowIfDisposed();
        _textureCore.Load(image);
        _safetyImpl.TryRegisterToCurrentContext(this, static x => x.DisposeContextAssociatedMemory());
    }

    /// <summary>Load pixel data filled with specified color</summary>
    /// <remarks>Texture width and height should be power of two for performance.</remarks>
    /// <param name="size">texture size</param>
    /// <param name="fill">color to fill all pixels with</param>
    public unsafe void Load(in Vector2i size, in ColorByte fill)
    {
        ThrowIfDisposed();
        _textureCore.Load(size, fill);
        _safetyImpl.TryRegisterToCurrentContext(this, static x => x.DisposeContextAssociatedMemory());
    }

    /// <summary>Create gpu texture buffer with specified size, but no uploading pixels. Pixels color remain undefined.</summary>
    /// <remarks>Texture width and height should be power of two for performance.</remarks>
    /// <param name="size">texture size</param>
    public void LoadUndefined(in Vector2i size)
    {
        ThrowIfDisposed();
        _textureCore.LoadUndefined(size);
        _safetyImpl.TryRegisterToCurrentContext(this, static x => x.DisposeContextAssociatedMemory());
    }

    public void Update(in Vector2i offset, in ReadOnlyImageRef subImage) => _textureCore.Update(offset, subImage);

    public void Update(in RectI rect, in ColorByte fill) => _textureCore.Update(rect, fill);

    public int GetPixels(in RectI rect, Span<ColorByte> buffer) => _textureCore.GetPixels(rect, buffer);

    public void GetPixels(in Vector2i offset, in ImageRef dest) => _textureCore.GetPixels(offset, dest);

    public void Dispose()
    {
        _safetyImpl.ThrowIfAssociatedContextMismatch();
        Dispose(true);
        GC.SuppressFinalize(this);
        _disposed = true;
    }

    private void Dispose(bool disposing)
    {
        if(disposing) {
            DisposeContextAssociatedMemory();
        }
        else {
            _safetyImpl.OnFinalized(this);
        }
    }

    private void DisposeContextAssociatedMemory()
    {
        _textureCore.Dispose();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ThrowIfDisposed()
    {
        if(_disposed) {
            Throw();
            [DoesNotReturn] static void Throw() => throw new ObjectDisposedException(nameof(Texture));
        }
    }
}
