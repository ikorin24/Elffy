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
        protected virtual string VertShaderSource =>    // TODO: private にすべき？
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

        protected virtual void DefineLocation(out string pos, out string uv)    // TODO: private にすべき？
        {
            pos = "_pos";
            uv = "_v_uv";
        }

        protected abstract void SendUniforms(Uniform uniform, in Vector2i screenSize, in TextureObject prerenderedTexture);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SendUniforms(PostProcessCompiled compiled)
        {
            SendUniforms(new Uniform(compiled.Program), compiled.ScreenSize, compiled.ColorBuffer);
        }

        protected virtual FrameBuffer CreateFrameBuffer()
        {
            return new FrameBuffer(FrameBuffer.BufferType.Texture,
                                   FrameBuffer.BufferType.RenderBuffer,
                                   FrameBuffer.BufferType.RenderBuffer);
        }

        internal PostProcessCompiled Compile()
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
                DefineLocation(out var pos, out var uv);
                var def = new VertexDefinition(program);
                def.Map<VertexSlim>(nameof(VertexSlim.Position), pos);
                def.Map<VertexSlim>(nameof(VertexSlim.UV), uv);
                VAO.Unbind();
                VBO.Unbind();
                return new PostProcessCompiled(this, program, vbo, ibo, vao, CreateFrameBuffer());
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
