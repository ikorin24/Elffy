#nullable enable
using Elffy.OpenGL;
using Elffy.Exceptions;
using Elffy.Components;
using System;
using OpenTK.Graphics.OpenGL4;

namespace Elffy
{
    public sealed class GBuffer : IDisposable
    {
        private const int MaxLightCount = 1024 * 1024;

        private FBO _fbo;
        private TextureObject _position;
        private TextureObject _normal;
        private TextureObject _color;
        private RBO _depth;
        private FloatDataTextureImpl _lights;
        private FloatDataTextureImpl _lightPositions;

        internal ref readonly FBO FBO => ref _fbo;
        internal ref readonly TextureObject Position => ref _position;
        internal ref readonly TextureObject Normal => ref _normal;
        internal ref readonly TextureObject Color => ref _color;
        internal ref readonly TextureObject Lights => ref _lights.TextureObject;
        internal ref readonly TextureObject LightPositions => ref _lightPositions.TextureObject;

        public DeferedRenderingPostProcess PostProcess { get; }

        public GBuffer(IHostScreen screen)
        {
            screen.ThrowIfNotMainThread();
            CreateGBuffer(screen.ClientSize, out _fbo, out _position, out _normal, out _color, out _depth);
            CreateLightsBuffer(out _lights, out _lightPositions);
            PostProcess = new DeferedRenderingPostProcess(this);
        }

        ~GBuffer() => Dispose(false);

        public void BindFrameBuffer()
        {
            FBO.Bind(_fbo, FBO.Target.FrameBuffer);
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
                _lights.Dispose();
                _lightPositions.Dispose();
            }
            else {
                throw new MemoryLeakException(typeof(GBuffer));
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
                TextureObject.Image2D(screenSize, (Color4*)null, TextureObject.InternalFormat.Rgba16f);
                TextureObject.Parameter2DMagFilter(TextureExpansionMode.NearestNeighbor);
                TextureObject.Parameter2DMinFilter(TextureShrinkMode.NearestNeighbor, TextureMipmapMode.None);
                TextureObject.Parameter2DWrapS(TextureWrapMode.ClampToEdge);
                TextureObject.Parameter2DWrapT(TextureWrapMode.ClampToEdge);
                FBO.SetTexture2DBuffer(position, FBO.Attachment.ColorAttachment0);
                bufs[0] = DrawBuffersEnum.ColorAttachment0;

                normal = TextureObject.Create();
                TextureObject.Bind2D(normal);
                TextureObject.Image2D(screenSize, (Color4*)null, TextureObject.InternalFormat.Rgba16f);
                TextureObject.Parameter2DMagFilter(TextureExpansionMode.NearestNeighbor);
                TextureObject.Parameter2DMinFilter(TextureShrinkMode.NearestNeighbor, TextureMipmapMode.None);
                TextureObject.Parameter2DWrapS(TextureWrapMode.ClampToEdge);
                TextureObject.Parameter2DWrapT(TextureWrapMode.ClampToEdge);
                FBO.SetTexture2DBuffer(normal, FBO.Attachment.ColorAttachment1);
                bufs[1] = DrawBuffersEnum.ColorAttachment1;

                color = TextureObject.Create();
                TextureObject.Bind2D(color);
                TextureObject.Image2D(screenSize, (ColorByte*)null, TextureObject.InternalFormat.Rgba8);
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

        private unsafe static void CreateLightsBuffer(out FloatDataTextureImpl lights, out FloatDataTextureImpl lightPositions)
        {
            lights = new();
            lights.LoadUndefined(MaxLightCount);
            lightPositions = new();
            lightPositions.LoadUndefined(MaxLightCount);
        }
    }
}
