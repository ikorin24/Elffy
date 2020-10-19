#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL4;
using Elffy.AssemblyServices;
using Elffy.OpenGL;
using Elffy.Shading;
using Elffy.Exceptions;

namespace Elffy.Core
{
    public abstract class PostProcess
    {
        protected abstract string VertShaderSource { get; }
        protected abstract string FragShaderSource { get; }

        protected abstract void DefineLocation(out string pos, out string uv);

        protected abstract void SendUniforms(Uniform uniform, in Vector2i screenSize, in TextureObject prerenderedTexture);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SendUniforms(PostProcessCompiled compiled)
        {
            SendUniforms(new Uniform(compiled.Program), compiled.ScreenSize, compiled.TextureObject);
        }

        internal PostProcessCompiled Compile()
        {
            // 0 - 3    polygon
            // | / |
            // 1 - 2

            ReadOnlySpan<VertexSlim> vertices = stackalloc VertexSlim[4]
            {
                new VertexSlim(new Vector3(-1, 1, 0),  new Vector2(0, 1)),
                new VertexSlim(new Vector3(-1, -1, 0), new Vector2(0, 0)),
                new VertexSlim(new Vector3(1, -1, 0),  new Vector2(1, 0)),
                new VertexSlim(new Vector3(1, 1, 0),   new Vector2(1, 1)),
            };
            ReadOnlySpan<int> indices = stackalloc int[6]
            {
                0, 1, 3, 1, 2, 3,
            };

            VBO vbo = default;
            IBO ibo = default;
            VAO vao = default;
            var program = ProgramObject.Empty;
            try {
                vbo = VBO.Create();
                VBO.BindBufferData(ref vbo, vertices, BufferUsageHint.StaticDraw);
                ibo = IBO.Create();
                IBO.BindBufferData(ref ibo, indices, BufferUsageHint.StaticDraw);
                vao = VAO.Create();
                VAO.Bind(vao);
                program = ShaderSource.CompileToProgramObject(VertShaderSource, FragShaderSource);
                DefineLocation(out var pos, out var uv);
                var def = new VertexDefinition(program);
                def.Map<VertexSlim>(nameof(VertexSlim.Position), pos);
                def.Map<VertexSlim>(nameof(VertexSlim.UV), uv);
                VAO.Unbind();
                VBO.Unbind();
                return new PostProcessCompiled(this, program, vbo, ibo, vao);
            }
            catch {
                VBO.Unbind();
                IBO.Unbind();
                VAO.Unbind();
                VBO.Delete(ref vbo);
                IBO.Delete(ref ibo);
                VAO.Delete(ref vao);
                ProgramObject.Delete(ref program);
                throw;
            }
        }
    }

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

        public PostProcessCompiled(PostProcess p, in ProgramObject program, in VBO vbo, in IBO ibo, in VAO vao)
        {
            Source = p;
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

        internal readonly ref struct Scope
        {
            private readonly PostProcessCompiled? _compiled;
            private readonly FBO _currentFbo;
            private readonly FBO _targetFbo;
            private readonly Vector2i _screenSize;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private Scope(PostProcessCompiled? compiled, in Vector2i screenSize)
            {
                // ctor for root scope.
                _compiled = compiled;
                _screenSize = screenSize;
                _targetFbo = FBO.Empty;     // Target fbo of root scope is 0, which means rendering to screen.
                if(compiled is null) {
                    // If post process is null, this does not change fbo.
                    // And do nothing on disposing.
                    _currentFbo = FBO.Empty;
                }
                else {
                    // Change fbo for post process.
                    // (and restore it when the scope is disposed.)
                    compiled.EnsureBuffer(_screenSize);
                    _targetFbo = FBO.Empty;
                    _currentFbo = compiled.FBO;
                    FBO.Bind(_currentFbo);
                }
                // Root scope always clear buffer
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private Scope(PostProcessCompiled? compiled, in Scope parentScope)
            {
                // ctor for inner scope.
                _compiled = compiled;
                _screenSize = parentScope._screenSize;
                if(compiled is null) {
                    // If post process is null, this does not change fbo.
                    // And do nothing on disposing.
                    _targetFbo = parentScope._targetFbo;
                    _currentFbo = parentScope._currentFbo;
                }
                else {
                    // Change fbo for post process.
                    // (and restore current fbo of parent scope when the scope is disposed.)
                    compiled.EnsureBuffer(_screenSize);
                    _targetFbo = parentScope._currentFbo;
                    _currentFbo = compiled.FBO;
                    FBO.Bind(_currentFbo);
                    // Clear buffer of switched fbo.
                    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal Scope NewScope(PostProcessCompiled? compiled)
            {
                return new Scope(compiled, this);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static Scope RootScope(PostProcessCompiled? compiled, in Vector2i screenSizes)
            {
                return new Scope(compiled, screenSizes);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                // Restore fbo and render post process.
                // Do nothing when post process is null.
                if(_compiled is null == false) {
                    RenderPostProcess();
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private void RenderPostProcess()
            {
                FBO.Bind(_targetFbo);
                var depthTestEnabled = GL.IsEnabled(EnableCap.DepthTest);
                GL.Disable(EnableCap.DepthTest);
                _compiled!.Render();
                if(depthTestEnabled) {
                    GL.Enable(EnableCap.DepthTest);
                }
            }
        }
    }

    public class FxaaPostProcessor : PostProcess
    {
        protected override string VertShaderSource => Vert;

        protected override string FragShaderSource => Frag;

        protected override void DefineLocation(out string pos, out string uv)
        {
            pos = "_pos";
            uv = "_uv";
        }

        protected override void SendUniforms(Uniform uniform, in Vector2i screenSize, in TextureObject prerenderedTexture)
        {
            uniform.SendTexture2D("_sampler", prerenderedTexture, TextureUnitNumber.Unit0);
            uniform.Send("_invScreenSize", new Vector2(1f / screenSize.X, 1f / screenSize.Y));
        }

        private const string Vert =
@"#version 440

in vec3 _pos;
in vec2 _uv;
out vec2 _uv2;

void main()
{
    _uv2 = _uv;
    gl_Position = vec4(_pos, 1.0);
}
";
        private const string Frag =
@"#version 440
" + GlslLibrary.FXAA + @"

in vec2 _uv2;
out vec4 _color;
uniform sampler2D _sampler;
uniform vec2 _invScreenSize;

void main()
{
    _color = FXAA(_sampler, _uv2, _invScreenSize);
}
";
    }
}
