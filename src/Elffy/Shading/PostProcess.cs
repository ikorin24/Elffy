#nullable enable
using System;
using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL4;
using Elffy.OpenGL;
using Elffy.Core;

namespace Elffy.Shading
{
    public abstract class PostProcess
    {
        private const string VertShaderSource =
@"#version 440
in vec3 _pos;
in vec2 _v_uv;
out vec2 _uv;
void main()
{
    _uv = _v_uv;
    gl_Position = vec4(_pos, 1.0);
}
";
        protected abstract string FragShaderSource { get; }

        private void DefineLocation(VertexDefinition definition)
        {
            definition.Map<VertexSlim>(nameof(VertexSlim.Position), "_pos");
            definition.Map<VertexSlim>(nameof(VertexSlim.UV), "_v_uv");
        }


        protected abstract void SendUniforms(Uniform uniform, in Vector2i screenSize);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SendUniforms(ProgramObject program, in Vector2i screenSize)
        {
            SendUniforms(new Uniform(program), screenSize);
        }

        [SkipLocalsInit]
        public PostProcessProgram Compile()
        {
            // 0 - 3    polygon
            // | / |
            // 1 - 2

            const float z = 0.9f;
            ReadOnlySpan<VertexSlim> vertices = stackalloc VertexSlim[4]
            {
                new VertexSlim(new Vector3(-1, 1, z),  new Vector2(0, 1)),
                new VertexSlim(new Vector3(-1, -1, z), new Vector2(0, 0)),
                new VertexSlim(new Vector3(1, -1, z),  new Vector2(1, 0)),
                new VertexSlim(new Vector3(1, 1, z),   new Vector2(1, 1)),
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
                DefineLocation(new VertexDefinition(program));
                VAO.Unbind();
                VBO.Unbind();
                return new PostProcessProgram(this, program, vbo, ibo, vao);
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
}
