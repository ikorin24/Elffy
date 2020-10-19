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
        private FBO _fbo;
        private TextureObject _to;
        private RBO _rbo;
        private VAO _vao;
        private VBO _vbo;
        private IBO _ibo;
        private ProgramObject _program;
        private Vector2i _screenSize;

        public PostProcess Source { get; }

        public ref readonly ProgramObject Program => ref _program;
        public ref readonly FBO FBO => ref _fbo;
        public ref readonly TextureObject TextureObject => ref _to;
        public ref readonly Vector2i ScreenSize => ref _screenSize;

        public PostProcessCompiled(PostProcess source, in ProgramObject program, in VBO vbo, in IBO ibo, in VAO vao)
        {
            Source = source;
            _program = program;
            _vbo = vbo;
            _ibo = ibo;
            _vao = vao;
        }

        ~PostProcessCompiled() => Dispose(false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureBuffer(in Vector2i screenSize)
        {
            if(screenSize == _screenSize) {
                return;
            }
            else {
                ChangeBuffer(screenSize);
            }
        }

        public void Render()
        {
            Debug.Assert(_fbo.IsEmpty == false);
            VAO.Bind(_vao);
            IBO.Bind(_ibo);
            ProgramObject.Bind(_program);
            Source.SendUniforms(this);
            GL.DrawElements(BeginMode.Triangles, _ibo.Length, DrawElementsType.UnsignedInt, 0);
            VAO.Unbind();
            IBO.Unbind();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ChangeBuffer(in Vector2i screenSize)
        {
            _screenSize = screenSize;
            if(screenSize.X <= 0 || screenSize.Y <= 0) {
                if(!_fbo.IsEmpty) {
                    Debug.Assert(_to.IsEmpty == false);
                    Debug.Assert(_rbo.IsEmpty == false);
                    FBO.Delete(ref _fbo);
                    TextureObject.Delete(ref _to);
                    RBO.Delete(ref _rbo);
                }
                return;
            }
            var fbo = FBO.Empty;
            var to = TextureObject.Empty;
            var rbo = RBO.Empty;
            try {
                fbo = FBO.Create();
                FBO.Bind(fbo);
                to = TextureObject.Create();
                TextureObject.Bind2D(to, TextureUnitNumber.Unit0);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, screenSize.X, screenSize.Y, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
                TextureObject.Unbind2D(TextureUnitNumber.Unit0);
                FBO.SetTexture2DBuffer(to);
                rbo = RBO.Create();
                RBO.Bind(rbo);
                RBO.SetStorage(screenSize.X, screenSize.Y);
                RBO.Unbind();
                FBO.SetRenderBuffer(rbo);
                FBO.Unbind();

                if(AssemblyState.IsDebug && !FBO.CheckStatus(out var status)) {
                    throw new Exception(status.ToString());
                }
            }
            catch {
                FBO.Delete(ref fbo);
                TextureObject.Delete(ref to);
                RBO.Delete(ref rbo);
                _screenSize = default;
                throw;
            }
            finally {
                // delete old buffers
                FBO.Delete(ref _fbo);
                TextureObject.Delete(ref _to);
                RBO.Delete(ref _rbo);
            }
            _fbo = fbo;
            _to = to;
            _rbo = rbo;
            Debug.Assert(_fbo.IsEmpty == false);
            Debug.Assert(_to.IsEmpty == false);
            Debug.Assert(_rbo.IsEmpty == false);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if(disposing) {
                FBO.Delete(ref _fbo);
                TextureObject.Delete(ref _to);
                RBO.Delete(ref _rbo);
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
}
