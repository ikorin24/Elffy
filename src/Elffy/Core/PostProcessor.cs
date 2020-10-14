#nullable enable
using System;
using System.Diagnostics;
using OpenTK.Graphics.OpenGL4;
using Elffy.AssemblyServices;
using Elffy.OpenGL;
using Elffy.Shading;
using System.IO;
using System.Runtime.CompilerServices;

namespace Elffy.Core
{
    public sealed class PostProcessor : IDisposable
    {
        private FBO _fbo;
        private TextureObject _to;
        private RBO _rbo;
        private Vector2i _screenSize;
        private Matrix4 _projection;

        private VAO _vao;
        private VBO _vbo;
        private IBO _ibo;
        private ProgramObject _program;
        private bool _initialized;


        private const string VertShaderSource =
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
        private const string FragShaderSource =
@"#version 440

in vec2 _uv2;
out vec4 _color;
uniform sampler2D _sampler;

void main()
{
    //_color = texture(_sampler, _uv2);
    _color = vec4(vec3(1.0, 1.0, 1.0) - texture(_sampler, _uv2).rgb, 1.0);
}
";

        internal PostProcessor()
        {
        }

        internal OffScreenRenderingScope OffScreenRendering(bool enabled, int width, int height)
        {
            if(!_initialized) {
                Init();
            }
            CreateBuffer(width, height);
            return new OffScreenRenderingScope(enabled, this);
        }

        private void Render()
        {
            const TextureUnitNumber textureUnit = TextureUnitNumber.Unit0;

            VAO.Bind(_vao);
            IBO.Bind(_ibo);
            TextureObject.Bind2D(_to, textureUnit);
            ProgramObject.Bind(_program);
            var uniform = new Uniform(_program);
            uniform.Send("_sampler", textureUnit);
            var depthTestEnabled = GL.IsEnabled(EnableCap.DepthTest);
            GL.Disable(EnableCap.DepthTest);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.DrawElements(BeginMode.Triangles, _ibo.Length, DrawElementsType.UnsignedInt, 0);
            if(depthTestEnabled) {
                GL.Enable(EnableCap.DepthTest);
            }
            VAO.Unbind();
            IBO.Unbind();
            TextureObject.Unbind2D(textureUnit);
        }

        private void CreateBuffer(int width, int height)
        {
            if(_screenSize.X == width && _screenSize.Y == height) {
                return;
            }

            if(width <= 0 || height <= 0) {
                if(!_fbo.IsEmpty) {
                    Debug.WriteLine(_to.IsEmpty == false);
                    Debug.WriteLine(_rbo.IsEmpty == false);

                    FBO.Delete(ref _fbo);
                    TextureObject.Delete(ref _to);
                    RBO.Delete(ref _rbo);
                    _screenSize = default;
                }
                return;
            }

            // Create new buffer of new size, and delete old one.
            FBO fbo;
            TextureObject to;
            RBO rbo;
            try {
                CreateNewBuffer(width, height, out fbo, out to, out rbo);
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
            _screenSize = new Vector2i(width, height);
            Matrix4.OrthographicProjection(0, 1, 0, 1, -1, 1, out _projection);

            static void CreateNewBuffer(int width, int height, out FBO fbo, out TextureObject to, out RBO rbo)
            {
                Debug.Assert(width > 0);
                Debug.Assert(height > 0);

                fbo = FBO.Create();
                {
                    FBO.Bind(fbo);
                    to = TextureObject.Create();
                    {
                        TextureObject.Bind2D(to, TextureUnitNumber.Unit0);
                        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, width, height, 0, PixelFormat.Rgb, PixelType.UnsignedByte, IntPtr.Zero);
                        TextureObject.Unbind2D(TextureUnitNumber.Unit0);
                    }
                    FBO.SetTexture2DBuffer(to);
                    rbo = RBO.Create();
                    {
                        RBO.Bind(rbo);
                        RBO.SetStorage(width, height);
                        RBO.Unbind();
                    }
                    FBO.SetRenderBuffer(rbo);
                    FBO.Unbind();
                }

                if(AssemblyState.IsDebug && !FBO.CheckStatus(out var status)) {
                    throw new Exception(status.ToString());
                }
            }
        }

        private void Init()
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
            _vbo = VBO.Create();
            VBO.BindBufferData(ref _vbo, vertices, BufferUsageHint.StaticDraw);
            _ibo = IBO.Create();
            IBO.BindBufferData(ref _ibo, indices, BufferUsageHint.StaticDraw);
            _vao = VAO.Create();
            VAO.Bind(_vao);

            try {
                _program = CompileShader(VertShaderSource, FragShaderSource);
            }
            catch {
                VBO.Unbind();
                IBO.Unbind();
                VAO.Unbind();
                VBO.Delete(ref _vbo);
                IBO.Delete(ref _ibo);
                VAO.Delete(ref _vao);
                throw;
            }

            var def = new VertexDefinition(_program);
            def.Map<VertexSlim>(nameof(VertexSlim.Position), "_pos");
            def.Map<VertexSlim>(nameof(VertexSlim.UV), "_uv");

            VAO.Unbind();
            VBO.Unbind();
            _initialized = true;


            static ProgramObject CompileShader(string vertSource, string fragSource)
            {
                int vertShader;
                vertShader = GL.CreateShader(ShaderType.VertexShader);
                GL.ShaderSource(vertShader, vertSource);
                GL.CompileShader(vertShader);
                GL.GetShader(vertShader, ShaderParameter.CompileStatus, out int vertCompileStatus);
                if(vertCompileStatus == Consts.ShaderCompileFailed) {
                    var log = GL.GetShaderInfoLog(vertShader);
                    throw new InvalidDataException($"Compiling shader is Failed.{Environment.NewLine}{log}{Environment.NewLine}{vertSource}");
                }

                int fragShader;
                fragShader = GL.CreateShader(ShaderType.FragmentShader);
                GL.ShaderSource(fragShader, fragSource);
                GL.CompileShader(fragShader);
                GL.GetShader(fragShader, ShaderParameter.CompileStatus, out int fragCompileStatus);
                if(fragCompileStatus == Consts.ShaderCompileFailed) {
                    var log = GL.GetShaderInfoLog(fragShader);
                    throw new InvalidDataException($"Compiling shader is Failed.{Environment.NewLine}{log}{Environment.NewLine}{fragSource}");
                }

                var program = ProgramObject.Create();
                GL.AttachShader(program.Value, vertShader);
                GL.AttachShader(program.Value, fragShader);
                GL.LinkProgram(program.Value);
                GL.GetProgram(program.Value, GetProgramParameterName.LinkStatus, out int linkStatus);
                if(linkStatus == Consts.ShaderProgramLinkFailed) {
                    var log = GL.GetProgramInfoLog(program.Value);
                    throw new InvalidOperationException($"Linking shader is failed.{Environment.NewLine}{log}");
                }

                return program;
            }
        }

        public void Dispose()
        {
            FBO.Delete(ref _fbo);
            TextureObject.Delete(ref _to);
            RBO.Delete(ref _rbo);
            VAO.Delete(ref _vao);
            VBO.Delete(ref _vbo);
            IBO.Delete(ref _ibo);
            ProgramObject.Delete(ref _program);
        }

        internal readonly ref struct OffScreenRenderingScope
        {
            private readonly bool _enabled;
            private readonly PostProcessor _p;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal OffScreenRenderingScope(bool enabled, PostProcessor p)
            {
                _enabled = enabled;
                _p = p;
                if(enabled) {
                    FBO.Bind(p._fbo);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
                if(_enabled) {
                    FBO.Unbind();
                    _p.Render();
                }
            }
        }
    }
}
