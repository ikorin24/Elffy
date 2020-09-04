#nullable enable
using System;
using System.Diagnostics;
using OpenToolkit.Graphics.OpenGL4;
using Elffy.AssemblyServices;
using Elffy.OpenGL;
using Elffy.Shading;
using System.IO;

namespace Elffy.Core
{
    public class PostProcessor : IDisposable
    {
        private FBO _fbo;
        private TextureObject _to;
        private RBO _rbo;


        private VAO _vao;
        private VBO _vbo;
        private IBO _ibo;
        private ProgramObject _program;
        private bool _initialized;


        private const string VertShaderSource =
@"#version 440

layout(location = 0) in vec3 _pos;
layout(location = 1) in vec2 _uv;
out vec2 uv_;
uniform mat4 _mvp;

void main()
{
    uv_ = _uv;
    gl_Position = _mvp * vec4(_pos, 1.0);
}
";
        private const string FragShaderSource =
@"#version 440

in vec2 uv_;
out vec4 _color;
uniform sampler2D _sampler;

void main()
{
    _color = texture(_sampler, uv_);
}
";

        internal PostProcessor()
        {
        }

        internal void EnableOffScreenRendering()
        {
            if(!_initialized) {
                Init();
                _initialized = true;
            }
            FBO.Bind(_fbo);
        }

        internal void DisableOffScreenRendering()
        {
            FBO.Unbind();
        }

        internal void RenderPostProcess(in Matrix4 projection)
        {
            // This method is called from a context which binded to fbo.

            VAO.Bind(_vao);
            IBO.Bind(_ibo);
            const TextureUnitNumber textureUnit = TextureUnitNumber.Unit0;
            TextureObject.Bind2D(_to, textureUnit);
            var uniform = new Uniform(_program);
            //uniform.Send(2, projection);                // location 2: matrix
            uniform.Send("_mvp", projection);                // location 2: matrix
            //uniform.Send(4, TextureUnitNumber.Unit0);   // location 4: texture sampler
            uniform.Send("_sampler", textureUnit);   // location 4: texture sampler
            GL.DrawElements(BeginMode.Triangles, _ibo.Length, DrawElementsType.UnsignedInt, 0);
            VAO.Unbind();
            IBO.Unbind();
            TextureObject.Unbind2D(textureUnit);
        }

        internal void CreateNewBuffer(int width, int height)
        {
            if(width <= 0 || height <= 0) {
                if(!_fbo.IsEmpty) {
                    FBO.Delete(ref _fbo);
                    TextureObject.Delete(ref _to);
                    RBO.Delete(ref _rbo);
                }
                return;
            }

            // Create new buffer of new size, and delete old one.
            FBO fbo;
            TextureObject to;
            RBO rbo;
            try {
                CreateBuffer(width, height, out fbo, out to, out rbo);
            }
            finally {
                FBO.Delete(ref _fbo);
                TextureObject.Delete(ref _to);
                RBO.Delete(ref _rbo);
            }
            _fbo = fbo;
            _to = to;
            _rbo = rbo;

            
            static void CreateBuffer(int width, int height, out FBO fbo, out TextureObject to, out RBO rbo)
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

            const int S = 1;
            ReadOnlySpan<float> vertices = stackalloc float[]
            {
                // [pos]        [uv]
                -S,  S, 0,      0, 1,   // 0
                -S, -S, 0,      0, 0,   // 1
                S,  -S, 0,      1, 0,   // 2
                S,  S,  0,      1, 1,   // 3
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
            catch(Exception) {
                VBO.Unbind();
                IBO.Unbind();
                VAO.Unbind();
                VBO.Delete(ref _vbo);
                IBO.Delete(ref _ibo);
                VAO.Delete(ref _vao);
                throw;
            }

            // vertex mapping
            // pos
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, sizeof(float) * 5, 0);
            // uv
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, sizeof(float) * 5, sizeof(float) * 3);

            VAO.Unbind();
            VBO.Unbind();


            static ProgramObject CompileShader(string vertSource, string fragSource)
            {
                int vertShader;
                vertShader = GL.CreateShader(ShaderType.VertexShader);
                GL.ShaderSource(vertShader, vertSource);
                GL.CompileShader(vertShader);
                GL.GetShader(vertShader, ShaderParameter.CompileStatus, out int vertCompileStatus);
                //Debug.Assert(vertCompileStatus != Consts.ShaderCompileFailed);
                if(vertCompileStatus == Consts.ShaderCompileFailed) {
                    var log = GL.GetShaderInfoLog(vertShader);
                    throw new InvalidDataException($"Compiling shader is Failed.{Environment.NewLine}{log}{Environment.NewLine}{vertSource}");
                }

                int fragShader;
                fragShader = GL.CreateShader(ShaderType.FragmentShader);
                GL.ShaderSource(fragShader, fragSource);
                GL.CompileShader(fragShader);
                GL.GetShader(fragShader, ShaderParameter.CompileStatus, out int fragCompileStatus);
                //Debug.Assert(fragCompileStatus != Consts.ShaderCompileFailed);
                if(fragCompileStatus == Consts.ShaderCompileFailed) {
                    var log = GL.GetShaderInfoLog(fragShader);
                    throw new InvalidDataException($"Compiling shader is Failed.{Environment.NewLine}{log}{Environment.NewLine}{fragSource}");
                }

                var program = ProgramObject.Create();
                GL.AttachShader(program.Value, vertShader);
                GL.AttachShader(program.Value, fragShader);
                GL.LinkProgram(program.Value);
                GL.GetProgram(program.Value, GetProgramParameterName.LinkStatus, out int linkStatus);
                //Debug.Assert(linkStatus != Consts.ShaderProgramLinkFailed);
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
    }
}
