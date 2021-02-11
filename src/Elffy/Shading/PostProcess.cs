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
@"#version 410
in vec3 _pos;
in vec2 _v_uv;
out vec2 _uv;
void main()
{
    _uv = _v_uv;
    gl_Position = vec4(_pos, 1.0);
}
";
        /// <summary>Get fragment shader code of glsl</summary>
        public abstract string FragShaderSource { get; }

        private void DefineLocation(VertexDefinition<VertexSlim> definition)
        {
            definition.Map("_pos", nameof(VertexSlim.Position));
            definition.Map("_v_uv", nameof(VertexSlim.UV));
        }

        /// <summary>Send uniform variables to glsl shader code.</summary>
        /// <param name="uniform">helper object to send uniform variables</param>
        /// <param name="screenSize">screen size</param>
        protected abstract void SendUniforms(Uniform uniform, in Vector2i screenSize);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SendUniformsInternal(ProgramObject program, in Vector2i screenSize)
        {
            SendUniforms(new Uniform(program), screenSize);
        }

        /// <summary>Compile post process fragment shader.</summary>
        /// <returns>compiled program of post process</returns>
        [SkipLocalsInit]
        public PostProcessProgram Compile()
        {
            // 0 - 3    polygon
            // | / |
            // 1 - 2

            const float z = 0.9f;
            ReadOnlySpan<VertexSlim> vertices = stackalloc VertexSlim[4]
            {
                new (new (-1, 1, z),  new (0, 1)),
                new (new (-1, -1, z), new (0, 0)),
                new (new (1, -1, z),  new (1, 0)),
                new (new (1, 1, z),   new (1, 1)),
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
                DefineLocation(new VertexDefinition<VertexSlim>(program));
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
