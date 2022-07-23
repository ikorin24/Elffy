#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using Elffy.Effective;
using Elffy.Graphics.OpenGL;
using Elffy.Features;
using Elffy.Components.Implementation;
using Elffy.Components;
using TextureWrapMode = Elffy.Components.TextureWrapMode;

namespace Elffy.Shading.Deferred
{
    internal sealed unsafe class GBuffer : IDisposable
    {
        // index  | format  | R        | G         | B        | A        |
        // ----
        // mrt[0] | Rgba16f | pos.x    | pos.y     | pos.z    | 1        |
        // mrt[1] | Rgba16f | normal.x | normal.y  | normal.z | 1        |
        // mrt[2] | Rgba16f | albedo.r | albedo.g  | albedo.b | 1        |
        // mrt[3] | Rgba16f | emit.r   | emit.g    | emit.b   | 1        |
        // mrt[4] | Rgba16f | metallic | roughness | 0        | 1        |

        private IHostScreen? _screen;
        private FBO _fbo;
        private GBufferMrt _mrt;
        private RBO _depth;
        private Vector2i _size;
        private bool _initialized;

        public bool IsInitialized => _initialized;

        public ref readonly FBO FBO => ref _fbo;

        public Vector2i Size => _size;

        private Span<TextureObject> Mrt => MemoryMarshal.CreateSpan(ref _mrt.Mrt0, GBufferMrt.MrtCount);

        public GBuffer()
        {
        }

        ~GBuffer() => Dispose(false);

        public bool TryGetScreen([MaybeNullWhen(false)] out IHostScreen screen)
        {
            screen = _screen;
            return _screen is not null;
        }

        public GBufferData GetBufferData()
        {
            return new GBufferData(_fbo, Mrt);
        }

        public void Initialize(IHostScreen screen)
        {
            if(screen is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(screen));
            }
            var currentContext = Engine.CurrentContext;
            if(currentContext != screen) {
                ContextMismatchException.Throw(currentContext, screen);
            }
            if(_initialized) {
                ThrowNotInitialized();
            }
            CreateGBuffer(screen.FrameBufferSize, out _fbo, out _depth, Mrt);
            _size = screen.FrameBufferSize;
            ContextAssociatedMemorySafety.Register(this, screen);
            _screen = screen;
            _initialized = true;
        }

        public unsafe void ClearColorBuffers()
        {
            float* clearColor = stackalloc float[4] { 0, 0, 0, 0 };
            for(int i = 0; i < GBufferMrt.MrtCount; i++) {
                GL.ClearBuffer(ClearBuffer.Color, i, clearColor);
            }
        }

        public void Resize()
        {
            if(TryGetScreen(out var screen) == false) {
                ThrowNotInitialized();
            }
            var currentContext = Engine.CurrentContext;
            if(currentContext != screen) {
                ContextMismatchException.Throw(currentContext, screen);
            }
            var newSize = screen.FrameBufferSize;
            if(newSize != _size) {
                DeleteResources();
                CreateGBuffer(screen.FrameBufferSize, out _fbo, out _depth, Mrt);
                _size = newSize;
            }
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
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
            var mrt = Mrt;
            for(int i = 0; i < mrt.Length; i++) {
                TextureObject.Delete(ref mrt[i]);
            }
            RBO.Delete(ref _depth);
        }

        private unsafe static void CreateGBuffer(Vector2i frameBufferSize,
                                                 out FBO fbo,
                                                 out RBO depth,
                                                 Span<TextureObject> mrt)
        {
            fbo = default;
            depth = default;
            try {
                frameBufferSize.X = Math.Max(1, frameBufferSize.X);
                frameBufferSize.Y = Math.Max(1, frameBufferSize.Y);
                fbo = FBO.Create();
                FBO.Bind(fbo, FBO.Target.FrameBuffer);
                var bufs = stackalloc DrawBuffersEnum[mrt.Length];
                for(int i = 0; i < mrt.Length; i++) {
                    CreateTextureObject(frameBufferSize, out mrt[i]);
                    bufs[i] = DrawBuffersEnum.ColorAttachment0 + i;
                    FBO.SetTexture2DColorAttachment(mrt[i], i);
                }

                depth = RBO.Create();
                RBO.Bind(depth);
                RBO.Storage(frameBufferSize, RBO.StorageType.Depth24Stencil8);
                FBO.SetRenderBufferDepthStencilAttachment(depth);
                FBO.ThrowIfInvalidStatus();
                GL.DrawBuffers(mrt.Length, bufs);
                TextureObject.Unbind2D();
            }
            catch {
                FBO.Delete(ref fbo);
                for(int i = 0; i < mrt.Length; i++) {
                    TextureObject.Delete(ref mrt[i]);
                }
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
                TextureObject.Parameter2DWrapS(TextureWrapMode.ClampToBorder);
                TextureObject.Parameter2DWrapT(TextureWrapMode.ClampToBorder);
            }
        }

        private unsafe static void CreateLightsBuffer(ReadOnlySpan<Vector4> positions, ReadOnlySpan<Color4> colors,
                                                      out FloatDataTextureCore lightColorsTexture, out FloatDataTextureCore lightPositionsTexture)
        {
            Debug.Assert(positions.Length == colors.Length);
            lightColorsTexture = new();
            lightPositionsTexture = new();
            try {
                lightColorsTexture.LoadAsPOT(colors);
                lightPositionsTexture.LoadAsPOT(positions.MarshalCast<Vector4, Color4>());
            }
            catch {
                lightColorsTexture.Dispose();
                lightPositionsTexture.Dispose();
                throw;
            }
        }

        [DoesNotReturn]
        private static void ThrowNotInitialized() => throw new InvalidOperationException($"{nameof(GBuffer)} is not initialized.");

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        private unsafe struct GBufferMrt
        {
            public const int MrtCount = 5;

            public TextureObject Mrt0;
            public TextureObject Mrt1;
            public TextureObject Mrt2;
            public TextureObject Mrt3;
            public TextureObject Mrt4;
        }
    }

    public readonly ref struct GBufferData
    {
        public FBO Fbo { get; }
        public ReadOnlySpan<TextureObject> Mrt { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GBufferData(FBO fbo, ReadOnlySpan<TextureObject> mrt)
        {
            Fbo = fbo;
            Mrt = mrt;
        }
    }
}
