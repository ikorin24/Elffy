#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL4;
using Elffy.Effective;
using Elffy.Graphics.OpenGL;
using Elffy.Features;
using Elffy.Components.Implementation;
using Elffy.Components;
using TextureWrapMode = Elffy.Components.TextureWrapMode;

namespace Elffy.Shading.Deferred
{
    internal sealed class GBuffer : IDisposable
    {
        private IHostScreen? _screen;
        private FBO _fbo;
        private TextureObject _position;            // Texture2D, Rgba16f, (x, y, z, 1)
        private TextureObject _normal;              // Texture2D, Rgba16f, (normal.x, normal.y, normal.z, 1)
        private TextureObject _albedo;      // Texture2D, Rgba16f, (albedo.r, albedo.g, albedo.b, 1)
        private TextureObject _emit;       // Texture2D, Rgba16f, (emit.r, emit.g, emit.b, 1)
        private TextureObject _metallicRoughness;   // Texture2D, Rgba16f, (metallic, roughness, 0, 1)
        private RBO _depth;
        private bool _initialized;

        public bool IsInitialized => _initialized;

        public ref readonly FBO FBO => ref _fbo;

        public GBuffer()
        {
        }

        ~GBuffer() => Dispose(false);

        public bool TryGetHostScreen([MaybeNullWhen(false)] out IHostScreen screen)
        {
            screen = _screen;
            return _screen is not null;
        }

        public GBufferData GetBufferData()
        {
            return new GBufferData(_fbo, _position, _normal, _albedo, _emit, _metallicRoughness);
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

            CreateGBuffer(screen.FrameBufferSize, out _fbo, out _position, out _normal,
                          out _albedo, out _emit, out _metallicRoughness, out _depth);
            ContextAssociatedMemorySafety.Register(this, screen);
            _screen = screen;
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
                _screen = null;
                FBO.Delete(ref _fbo);
                TextureObject.Delete(ref _position);
                TextureObject.Delete(ref _normal);
                TextureObject.Delete(ref _albedo);
                TextureObject.Delete(ref _emit);
                TextureObject.Delete(ref _metallicRoughness);
                RBO.Delete(ref _depth);
            }
            else {
                ContextAssociatedMemorySafety.OnFinalized(this);
            }
        }

        private unsafe static void CreateGBuffer(in Vector2i frameBufferSize,
                                                 out FBO fbo,
                                                 out TextureObject position,
                                                 out TextureObject normal,
                                                 out TextureObject albedo,
                                                 out TextureObject emit,
                                                 out TextureObject metallicRoughness,
                                                 out RBO depth)
        {
            fbo = default;
            position = default;
            normal = default;
            albedo = default;
            emit = default;
            metallicRoughness = default;
            depth = default;
            try {
                fbo = FBO.Create();
                FBO.Bind(fbo, FBO.Target.FrameBuffer);
                const int bufCount = 5; // buffer count except depth buffer
                var bufs = stackalloc DrawBuffersEnum[bufCount];

                CreateTextureObject(frameBufferSize, out position);
                FBO.SetTexture2DBuffer(position, FBO.Attachment.ColorAttachment0);
                bufs[0] = DrawBuffersEnum.ColorAttachment0;

                CreateTextureObject(frameBufferSize, out normal);
                FBO.SetTexture2DBuffer(normal, FBO.Attachment.ColorAttachment1);
                bufs[1] = DrawBuffersEnum.ColorAttachment1;

                CreateTextureObject(frameBufferSize, out albedo);
                FBO.SetTexture2DBuffer(albedo, FBO.Attachment.ColorAttachment2);
                bufs[2] = DrawBuffersEnum.ColorAttachment2;

                CreateTextureObject(frameBufferSize, out emit);
                FBO.SetTexture2DBuffer(emit, FBO.Attachment.ColorAttachment3);
                bufs[3] = DrawBuffersEnum.ColorAttachment3;

                CreateTextureObject(frameBufferSize, out metallicRoughness);
                FBO.SetTexture2DBuffer(metallicRoughness, FBO.Attachment.ColorAttachment4);
                bufs[4] = DrawBuffersEnum.ColorAttachment4;

                depth = RBO.Create();
                RBO.Bind(depth);
                RBO.Storage(frameBufferSize, RBO.StorageType.Depth24Stencil8);
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
                TextureObject.Delete(ref albedo);
                TextureObject.Delete(ref emit);
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
                TextureObject.Parameter2DWrapS(TextureWrapMode.ClampToEdge);
                TextureObject.Parameter2DWrapT(TextureWrapMode.ClampToEdge);
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
    }

    internal readonly ref struct GBufferData
    {
        public readonly FBO FBO;
        /// <summary>Texture2D, Rgba16f, (x, y, z, 1)</summary>
        public readonly TextureObject Position;
        /// <summary>Texture2D, Rgba16f, (normal.x, normal.y, normal.z, 1)</summary>
        public readonly TextureObject Normal;
        /// <summary>Texture2D, Rgba16f, (albedo.r, albedo.g, albedo.b, 1)</summary>
        public readonly TextureObject Albedo;
        /// <summary>Texture2D, Rgba16f, (emit.r, emit.g, emit.b, 1)</summary>
        public readonly TextureObject Emit;
        /// <summary>Texture2D, Rgba16f, (metallic, roughness, 0, 1)</summary>
        public readonly TextureObject MetallicRoughness;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GBufferData(in FBO fbo, in TextureObject position, in TextureObject normal,
                           in TextureObject albedoMetallic, in TextureObject emitRoughness,
                           in TextureObject metallicRoughness)
        {
            FBO = fbo;
            Position = position;
            Normal = normal;
            Albedo = albedoMetallic;
            Emit = emitRoughness;
            MetallicRoughness = metallicRoughness;
        }
    }
}
