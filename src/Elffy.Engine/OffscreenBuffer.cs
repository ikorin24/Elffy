#nullable enable
using Elffy.Features;
using Elffy.Graphics.OpenGL;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy;

public sealed class OffscreenBuffer : IOffscreenBuffer, IDisposable
{
    private IHostScreen? _screen;
    private FBO _fbo;
    private RBO _depth;
    private TextureObject _renderTarget;
    private Vector2i _size;
    private bool _initialized;

    // Depth buffer should be cleared as 1 instead of 0.
    private const float DepthClearValue = 1f;
    private const int StencilClearValue = 0;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private unsafe static float* ClearColor
    {
        get
        {
            // The following two lines mean `const float clearColor[4] = { 0 }` in C++
            ReadOnlySpan<byte> ConstZeroFloat4 = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            return (float*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(ConstZeroFloat4));
        }
    }

    public bool IsInitialized => _initialized;

    public FBO FBO => _fbo;
    public TextureObject RenderTargetTexture => _renderTarget;

    public Vector2i Size => _size;

    public OffscreenBuffer()
    {
    }

    ~OffscreenBuffer() => Dispose(false);

    public bool TryGetScreen([MaybeNullWhen(false)] out IHostScreen screen)
    {
        screen = _screen;
        return _screen is not null;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    public void Initialize(IHostScreen screen)
    {
        ArgumentNullException.ThrowIfNull(screen);
        var currentContext = Engine.CurrentContext;
        if(currentContext != screen) {
            ContextMismatchException.Throw(currentContext, screen);
        }
        if(_initialized) {
            ThrowNotInitialized();
        }
        Create(screen.FrameBufferSize, out _fbo, out _depth, out _renderTarget);
        _size = screen.FrameBufferSize;
        ContextAssociatedMemorySafety.Register(this, screen);
        _screen = screen;
        _initialized = true;
    }

    public unsafe void ClearAllBuffers()
    {
        if(_fbo.IsEmpty) { return; }
        using(var _ = FBO.PreserveCurrentBinded()) {
            FBO.Bind(_fbo, FBO.Target.FrameBuffer);
            GL.ClearBuffer(ClearBuffer.Color, 0, ClearColor);
            GL.ClearBuffer(ClearBufferCombined.DepthStencil, 0, DepthClearValue, StencilClearValue);
        }
    }

    public unsafe void ClearColorBuffer()
    {
        if(_fbo.IsEmpty) { return; }
        using(var _ = FBO.PreserveCurrentBinded()) {
            FBO.Bind(_fbo, FBO.Target.FrameBuffer);
            GL.ClearBuffer(ClearBuffer.Color, 0, ClearColor);
        }
    }

    public unsafe void ClearDepthStencilBuffer()
    {
        if(_fbo.IsEmpty) { return; }

        using(var _ = FBO.PreserveCurrentBinded()) {
            FBO.Bind(_fbo, FBO.Target.FrameBuffer);
            GL.ClearBuffer(ClearBufferCombined.DepthStencil, 0, DepthClearValue, StencilClearValue);
        }
    }

    public void Resize()
    {
        var screen = _screen;
        if(screen == null) {
            ThrowNotInitialized();
        }
        var currentContext = Engine.CurrentContext;
        if(currentContext != screen) {
            ContextMismatchException.Throw(currentContext, screen);
        }
        var newSize = screen.FrameBufferSize;
        if(newSize != _size) {
            DeleteResources();
            Create(screen.FrameBufferSize, out _fbo, out _depth, out _renderTarget);
            _size = newSize;
        }
    }

    private void Dispose(bool disposing)
    {
        if(_fbo.IsEmpty) { return; }
        if(disposing) {
            _screen = null;
            DeleteResources();
        }
        else {
            ContextAssociatedMemorySafety.OnFinalized(this);
        }
    }

    private void DeleteResources()
    {
        FBO.Delete(ref _fbo);
        TextureObject.Delete(ref _renderTarget);
        RBO.Delete(ref _depth);
    }

    private unsafe static void Create(Vector2i frameBufferSize, out FBO fbo, out RBO depth, out TextureObject renderTarget)
    {
        fbo = default;
        depth = default;
        renderTarget = default;
        try {
            frameBufferSize.X = Math.Max(1, frameBufferSize.X);
            frameBufferSize.Y = Math.Max(1, frameBufferSize.Y);
            fbo = FBO.Create();
            FBO.Bind(fbo, FBO.Target.FrameBuffer);
            CreateTextureObject(frameBufferSize, out renderTarget);
            FBO.SetTexture2DColorAttachment(renderTarget, 0);

            depth = RBO.Create();
            RBO.Bind(depth);
            RBO.Storage(frameBufferSize, RBO.StorageType.Depth24Stencil8);
            FBO.SetRenderBufferDepthStencilAttachment(depth);
            FBO.ThrowIfInvalidStatus();
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
            TextureObject.Unbind2D();
        }
        catch {
            FBO.Delete(ref fbo);
            TextureObject.Delete(ref renderTarget);
            RBO.Delete(ref depth);
            FBO.Unbind(FBO.Target.FrameBuffer);
            throw;
        }

        static void CreateTextureObject(in Vector2i frameBufferSize, out TextureObject to)
        {
            to = TextureObject.Create();
            TextureObject.Bind2D(to);
            TextureObject.Image2D(frameBufferSize, (Color4*)null, TextureInternalFormat.Rgba16f, 0);
            TextureObject.Parameter2DMagFilter(TextureExpansionMode.NearestNeighbor);
            TextureObject.Parameter2DMinFilter(TextureShrinkMode.NearestNeighbor, TextureMipmapMode.None);
            TextureObject.Parameter2DWrapS(TextureWrap.ClampToBorder);
            TextureObject.Parameter2DWrapT(TextureWrap.ClampToBorder);
        }
    }

    [DoesNotReturn]
    private static void ThrowNotInitialized() => throw new InvalidOperationException($"{nameof(OffscreenBuffer)} is not initialized.");
}

public interface IOffscreenBuffer
{
    bool IsInitialized { get; }
    FBO FBO { get; }
    TextureObject RenderTargetTexture { get; }
    Vector2i Size { get; }
}
