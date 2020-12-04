#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL4;
using Elffy.OpenGL;
using Elffy.Exceptions;
using Elffy.AssemblyServices;

namespace Elffy.Shading
{
    internal sealed class PostProcessCompiled : IDisposable
    {
        private FrameBuffer _frameBuffer;
        private VAO _vao;
        private VBO _vbo;
        private IBO _ibo;
        private ProgramObject _program;
        private Vector2i _screenSize;

        public PostProcess Source { get; }

        public ref readonly ProgramObject Program => ref _program;
        public ref readonly Vector2i ScreenSize => ref _screenSize;
        public ref readonly TextureObject ColorBuffer
        {
            get
            {
                if(_frameBuffer.ColorType != FrameBuffer.BufferType.Texture) {
                    ThrowInvalid();
                    static void ThrowInvalid() => throw new InvalidOperationException();
                }
                return ref _frameBuffer.ColorTo;
            }
        }

        public ref readonly FBO FBO => ref _frameBuffer.Fbo;

        public PostProcessCompiled(PostProcess source, in ProgramObject program, in VBO vbo, in IBO ibo, in VAO vao, in FrameBuffer frameBuffer)
        {
            Source = source;
            _program = program;
            _vbo = vbo;
            _ibo = ibo;
            _vao = vao;
            _frameBuffer = frameBuffer;
        }

        ~PostProcessCompiled() => Dispose(false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly FBO GetFBO(in Vector2i screenSize)
        {
            if(screenSize != _screenSize) {
                _frameBuffer.CreateBuffer(screenSize);
                _screenSize = screenSize;
            }
            return ref _frameBuffer.Fbo;
        }

        public void Render()
        {
            Debug.Assert(_frameBuffer.IsEmpty == false);
            VAO.Bind(_vao);
            IBO.Bind(_ibo);
            ProgramObject.Bind(_program);
            Source.SendUniforms(this);
            GL.DrawElements(BeginMode.Triangles, _ibo.Length, DrawElementsType.UnsignedInt, 0);
            VAO.Unbind();
            IBO.Unbind();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if(disposing) {
                _screenSize = default;
                _frameBuffer.Dispose();
                VAO.Delete(ref _vao);
                VBO.Delete(ref _vbo);
                IBO.Delete(ref _ibo);
                ProgramObject.Delete(ref _program);
            }
            else {
                // We cannot release opengl resources from the GC thread.
                throw new MemoryLeakException(GetType());
            }
        }
    }

    public struct FrameBuffer : IDisposable
    {
        public FBO Fbo;
        public RBO ColorRbo;
        public RBO StencilRbo;
        public RBO DepthRbo;
        public TextureObject ColorTo;
        public TextureObject StencilTo;
        public TextureObject DepthTo;
        public RBO StencilDepthRbo;

        public BufferType ColorType { get; }
        public BufferType StencilType { get; }
        public BufferType DepthType { get; }

        public bool DepthAndStencilMerged { get; }

        public bool IsEmpty => Fbo.IsEmpty;

        public FrameBuffer(BufferType color, bool depthAndStencilMerged)
        {
            ColorType = color;
            StencilType = BufferType.RenderBuffer;
            DepthType = BufferType.RenderBuffer;
            DepthAndStencilMerged = depthAndStencilMerged;

            Fbo = default;
            ColorRbo = default;
            StencilRbo = default;
            DepthRbo = default;
            ColorTo = default;
            StencilTo = default;
            DepthTo = default;
            StencilDepthRbo = default;
        }

        public FrameBuffer(BufferType color, BufferType stencil, BufferType depth)
        {
            ColorType = color;
            StencilType = stencil;
            DepthType = depth;
            DepthAndStencilMerged = false;

            Fbo = default;
            ColorRbo = default;
            StencilRbo = default;
            DepthRbo = default;
            ColorTo = default;
            StencilTo = default;
            DepthTo = default;
            StencilDepthRbo = default;
        }

        public void CreateBuffer(in Vector2i screenSize)
        {
            if(screenSize.X <= 0 || screenSize.Y <= 0) {
                Dispose();
                return;
            }

            FBO fbo = default;
            RBO colorRbo = default;
            RBO stencilRbo = default;
            RBO depthRbo = default;
            TextureObject colorTo = default;
            TextureObject stencilTo = default;
            TextureObject depthTo = default;
            RBO stencilDepthRbo = default;

            try {
                fbo = FBO.Create();
                FBO.Bind(fbo, FBO.Target.FrameBuffer);

                switch(ColorType) {
                    case BufferType.Texture: {
                        CreateTexture(screenSize, out colorTo);
                        FBO.SetTexture2DBuffer(colorTo, FBO.Attachment.ColorAttachment0);
                        break;
                    }
                    case BufferType.RenderBuffer: {
                        colorRbo = RBO.Create();
                        RBO.Bind(colorRbo);
                        RBO.SetStorage(screenSize.X, screenSize.Y, RBO.StorageType.Rgba32f);
                        RBO.Unbind();
                        FBO.SetRenderBuffer(colorRbo, FBO.Attachment.ColorAttachment0);
                        break;
                    }
                    default:
                        break;
                }
                if(DepthAndStencilMerged) {
                    Debug.Assert(StencilType == BufferType.RenderBuffer);
                    Debug.Assert(DepthType == BufferType.RenderBuffer);

                    stencilDepthRbo = RBO.Create();
                    RBO.Bind(stencilDepthRbo);
                    RBO.SetStorage(screenSize.X, screenSize.Y, RBO.StorageType.Stencil24Stencil8);
                    RBO.Unbind();
                    FBO.SetRenderBuffer(stencilDepthRbo, FBO.Attachment.DepthStencilAttachment);
                }
                else {
                    switch(StencilType) {
                        case BufferType.Texture: {
                            CreateTexture(screenSize, out stencilTo);
                            FBO.SetTexture2DBuffer(stencilTo, FBO.Attachment.StencilAttachment);
                            break;
                        }
                        case BufferType.RenderBuffer: {
                            stencilRbo = RBO.Create();
                            RBO.Bind(stencilRbo);
                            RBO.SetStorage(screenSize.X, screenSize.Y, RBO.StorageType.Stencil1);
                            RBO.Unbind();
                            FBO.SetRenderBuffer(stencilRbo, FBO.Attachment.StencilAttachment);
                            break;
                        }
                        default:
                            break;
                    }

                    switch(DepthType) {
                        case BufferType.Texture: {
                            CreateTexture(screenSize, out depthTo);
                            FBO.SetTexture2DBuffer(depthTo, FBO.Attachment.DepthAttachment);
                            break;
                        }
                        case BufferType.RenderBuffer: {
                            depthRbo = RBO.Create();
                            RBO.Bind(depthRbo);
                            RBO.SetStorage(screenSize.X, screenSize.Y, RBO.StorageType.Depth24);
                            RBO.Unbind();
                            FBO.SetRenderBuffer(depthRbo, FBO.Attachment.DepthAttachment);
                            break;
                        }
                        default:
                            break;
                    }
                }
                if(AssemblyState.IsDebug && !FBO.CheckStatus(out var error)) {
                    throw new Exception(error);
                }
            }
            catch {

                FBO.Delete(ref fbo);
                RBO.Delete(ref colorRbo);
                RBO.Delete(ref stencilRbo);
                RBO.Delete(ref depthRbo);
                TextureObject.Delete(ref colorTo);
                TextureObject.Delete(ref stencilTo);
                TextureObject.Delete(ref depthTo);
                RBO.Delete(ref stencilDepthRbo);
                throw;
            }
            finally {
                // delete old buffers
                Dispose();
            }

            Fbo = fbo;
            ColorRbo = colorRbo;
            StencilRbo = stencilRbo;
            DepthRbo = depthRbo;
            ColorTo = colorTo;
            StencilTo = stencilTo;
            DepthTo = depthTo;
            StencilDepthRbo = stencilDepthRbo;
        }

        private static unsafe void CreateTexture(in Vector2i screenSize, out TextureObject to)
        {
            to = TextureObject.Create();
            TextureObject.Bind2D(to, TextureUnitNumber.Unit0);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            TextureObject.Parameter2DMagFilter(TextureExpansionMode.Bilinear);
            TextureObject.Parameter2DMinFilter(TextureShrinkMode.Bilinear);
            TextureObject.Image2D(screenSize, (ColorByte*)null);
            TextureObject.Unbind2D(TextureUnitNumber.Unit0);
        }

        public void Dispose()
        {
            FBO.Delete(ref Fbo);
            RBO.Delete(ref ColorRbo);
            RBO.Delete(ref StencilRbo);
            RBO.Delete(ref DepthRbo);
            TextureObject.Delete(ref ColorTo);
            TextureObject.Delete(ref StencilTo);
            TextureObject.Delete(ref DepthTo);
            RBO.Delete(ref StencilDepthRbo);
        }

        public enum BufferType
        {
            None,
            Texture,
            RenderBuffer,
        }
    }
}
