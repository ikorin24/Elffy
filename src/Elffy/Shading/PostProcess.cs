#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL4;
using Elffy.AssemblyServices;
using Elffy.OpenGL;
using Elffy.Core;
using Elffy.Exceptions;

namespace Elffy.Shading
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

    public sealed class FxaaPostProcess : PostProcess
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
