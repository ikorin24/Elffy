#nullable enable
using System;
using Elffy.Graphics.OpenGL;
using Elffy.Imaging;
using Elffy.Features;
using Elffy.Features.Implementation;

namespace Elffy;

public sealed class Texture : IContextAssociatedSafety
{
    private ContextAssociatedSafetyImpl _safetyImpl;    // Mutable object, Don't change into readonly
    private TextureCore _textureCore;                   // Mutable object, Don't change into readonly

    public TextureExpansionMode ExpansionMode => _textureCore.ExpansionMode;
    public TextureShrinkMode ShrinkMode => _textureCore.ShrinkMode;
    public TextureMipmapMode MipmapMode => _textureCore.MipmapMode;
    public TextureWrapMode WrapModeX => _textureCore.WrapModeX;
    public TextureWrapMode WrapModeY => _textureCore.WrapModeY;
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

    ~Texture() => _safetyImpl.OnFinalized();

    public void Load<T>(in Vector2i size, ImageBuilderDelegate<T> imageBuilder, T state)
    {
        _textureCore.Load(state, size, imageBuilder);
        _safetyImpl.TryRegisterToCurrentContext(this, static x => x.DisposeContextAssociatedMemory());
    }

    /// <summary>Load image</summary>
    /// <param name="image">image to load</param>
    public void Load(in ReadOnlyImageRef image)
    {
        _textureCore.Load(image);
        _safetyImpl.TryRegisterToCurrentContext(this, static x => x.DisposeContextAssociatedMemory());
    }

    /// <summary>Load pixel data filled with specified color</summary>
    /// <remarks>Texture width and height should be power of two for performance.</remarks>
    /// <param name="size">texture size</param>
    /// <param name="fill">color to fill all pixels with</param>
    public unsafe void Load(in Vector2i size, in ColorByte fill)
    {
        _textureCore.Load(size, fill);
        _safetyImpl.TryRegisterToCurrentContext(this, static x => x.DisposeContextAssociatedMemory());
    }

    /// <summary>Create gpu texture buffer with specified size, but no uploading pixels. Pixels color remain undefined.</summary>
    /// <remarks>Texture width and height should be power of two for performance.</remarks>
    /// <param name="size">texture size</param>
    public void LoadUndefined(in Vector2i size)
    {
        _textureCore.LoadUndefined(size);
        _safetyImpl.TryRegisterToCurrentContext(this, static x => x.DisposeContextAssociatedMemory());
    }

    public void Update(in Vector2i offset, in ReadOnlyImageRef subImage) => _textureCore.Update(offset, subImage);

    public void Update(in RectI rect, in ColorByte fill) => _textureCore.Update(rect, fill);

    public int GetPixels(in RectI rect, Span<ColorByte> buffer) => _textureCore.GetPixels(rect, buffer);

    public void GetPixels(in Vector2i offset, in ImageRef dest) => _textureCore.GetPixels(offset, dest);

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        if(disposing && _safetyImpl.IsAssociatedWithCurrentContext()) {
            DisposeContextAssociatedMemory();
        }
        else {
            _safetyImpl.OnFinalized();
        }
    }

    private void DisposeContextAssociatedMemory()
    {
        _textureCore.Dispose();
    }
}

public interface IContextAssociatedSafety : IDisposable
{
    IHostScreen? AssociatedContext { get; }

    bool IsAssociatedWithCurrentContext()
    {
        var current = Engine.CurrentContext;
        return current != null && current == AssociatedContext;
    }
}

public struct ContextAssociatedSafetyImpl
{
    private bool _isSafetyRegistered;
    private IHostScreen? _associatedContext;
    private ContextAssociatedMemorySafety.SafetyKey _safety;

    public readonly IHostScreen? AssociatedContext => _associatedContext;

    public readonly bool IsAssociatedWithCurrentContext()
    {
        var current = Engine.CurrentContext;
        return current != null && current == _associatedContext;
    }

    public bool TryRegister<T>(T obj, IHostScreen screen, Action<T> release) where T : class, IContextAssociatedSafety
    {
        if(obj == null) { return false; }
        if(screen == null) { return false; }
        if(_isSafetyRegistered) {
            return false;
        }
        _safety = ContextAssociatedMemorySafety.RegisterNonDisposable(obj, release, screen);
        _associatedContext = screen;
        _isSafetyRegistered = true;
        return true;
    }

    public bool TryRegisterToCurrentContext<T>(T obj, Action<T> disposeAction) where T : class, IContextAssociatedSafety
    {
        if(obj == null) { return false; }
        var screen = Engine.CurrentContext;
        if(screen == null) { return false; }
        return TryRegister(obj, screen, disposeAction);
    }

    public readonly void OnFinalized()
    {
        if(_isSafetyRegistered) {
            ContextAssociatedMemorySafety.OnFinalized(_safety);
        }
    }
}
