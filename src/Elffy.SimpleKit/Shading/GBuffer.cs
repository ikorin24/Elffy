#nullable enable
using Elffy.Core;
using Elffy.Components;
using Elffy.Effective;
using Elffy.OpenGL;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL4;

namespace Elffy.Shading
{
    internal sealed class GBuffer : IGBuffer, IDisposable
    {
        private FBO _fbo;
        private TextureObject _position;
        private TextureObject _normal;
        private TextureObject _color;
        private RBO _depth;
        private bool _initialized;

        public ref readonly FBO FBO => ref _fbo;

        public GBuffer()
        {
        }

        ~GBuffer() => Dispose(false);

        public GBufferData GetBufferData()
        {
            return new GBufferData(_fbo, _position, _normal, _color);
        }

        public void Initialize(IHostScreen screen)
        {
            if(screen is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(screen));
            }
            if(Engine.CurrentContext != screen) {
                ThrowInvalidContext();
                [DoesNotReturn] static void ThrowInvalidContext() => throw new InvalidOperationException("Invalid current context");
            }
            if(_initialized) {
                ThrowNotInitialized();
            }

            CreateGBuffer(screen.FrameBufferSize, out _fbo, out _position, out _normal, out _color, out _depth);
            ContextAssociatedMemorySafety.Register(this, screen);
            _initialized = true;
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
                FBO.Delete(ref _fbo);
                TextureObject.Delete(ref _position);
                TextureObject.Delete(ref _normal);
                TextureObject.Delete(ref _color);
                RBO.Delete(ref _depth);
            }
            else {
                ContextAssociatedMemorySafety.OnFinalized(this);
            }
        }

        private unsafe static void CreateGBuffer(in Vector2i screenSize,
                                                 out FBO fbo,
                                                 out TextureObject position,
                                                 out TextureObject normal,
                                                 out TextureObject color,
                                                 out RBO depth)
        {
            fbo = default;
            position = default;
            normal = default;
            color = default;
            depth = default;
            try {
                fbo = FBO.Create();
                FBO.Bind(fbo, FBO.Target.FrameBuffer);
                const int bufCount = 3; // buffer count except depth buffer
                var bufs = stackalloc DrawBuffersEnum[bufCount];

                position = TextureObject.Create();
                TextureObject.Bind2D(position);
                TextureObject.Image2D(screenSize, (Color4*)null, TextureInternalFormat.Rgba16f, 0);
                TextureObject.Parameter2DMagFilter(TextureExpansionMode.NearestNeighbor);
                TextureObject.Parameter2DMinFilter(TextureShrinkMode.NearestNeighbor, TextureMipmapMode.None);
                TextureObject.Parameter2DWrapS(TextureWrapMode.ClampToEdge);
                TextureObject.Parameter2DWrapT(TextureWrapMode.ClampToEdge);
                FBO.SetTexture2DBuffer(position, FBO.Attachment.ColorAttachment0);
                bufs[0] = DrawBuffersEnum.ColorAttachment0;

                normal = TextureObject.Create();
                TextureObject.Bind2D(normal);
                TextureObject.Image2D(screenSize, (Color4*)null, TextureInternalFormat.Rgba16f, 0);
                TextureObject.Parameter2DMagFilter(TextureExpansionMode.NearestNeighbor);
                TextureObject.Parameter2DMinFilter(TextureShrinkMode.NearestNeighbor, TextureMipmapMode.None);
                TextureObject.Parameter2DWrapS(TextureWrapMode.ClampToEdge);
                TextureObject.Parameter2DWrapT(TextureWrapMode.ClampToEdge);
                FBO.SetTexture2DBuffer(normal, FBO.Attachment.ColorAttachment1);
                bufs[1] = DrawBuffersEnum.ColorAttachment1;

                color = TextureObject.Create();
                TextureObject.Bind2D(color);
                TextureObject.Image2D(screenSize, (ColorByte*)null, TextureInternalFormat.Rgba8, 0);
                TextureObject.Parameter2DMagFilter(TextureExpansionMode.NearestNeighbor);
                TextureObject.Parameter2DMinFilter(TextureShrinkMode.NearestNeighbor, TextureMipmapMode.None);
                TextureObject.Parameter2DWrapS(TextureWrapMode.ClampToEdge);
                TextureObject.Parameter2DWrapT(TextureWrapMode.ClampToEdge);
                FBO.SetTexture2DBuffer(color, FBO.Attachment.ColorAttachment2);
                bufs[2] = DrawBuffersEnum.ColorAttachment2;

                depth = RBO.Create();
                RBO.Bind(depth);
                RBO.Storage(screenSize, RBO.StorageType.Depth24Stencil8);
                FBO.SetRenderBuffer(depth, FBO.Attachment.DepthStencilAttachment);

                if(!FBO.CheckStatus(out var error)) {
                    throw new Exception(error);
                }
                GL.DrawBuffers(bufCount, bufs);
                TextureObject.Unbind2D();
            }
            catch {
                FBO.Delete(ref fbo);
                TextureObject.Delete(ref position);
                TextureObject.Delete(ref normal);
                TextureObject.Delete(ref color);
                RBO.Delete(ref depth);
                FBO.Unbind(FBO.Target.FrameBuffer);
                throw;
            }
        }

        private unsafe static void CreateLightsBuffer(ReadOnlySpan<Vector4> positions, ReadOnlySpan<Color4> colors,
                                                      out FloatDataTextureImpl lightColorsTexture, out FloatDataTextureImpl lightPositionsTexture)
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
    }

    internal interface IGBuffer
    {
        GBufferData GetBufferData();
    }

    internal readonly ref struct GBufferData
    {
        public readonly FBO FBO;
        public readonly TextureObject Position;
        public readonly TextureObject Normal;
        public readonly TextureObject Color;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GBufferData(in FBO fbo, in TextureObject position, in TextureObject normal, in TextureObject color)
        {
            FBO = fbo;
            Position = position;
            Normal = normal;
            Color = color;
        }
    }
}
