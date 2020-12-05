#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elffy.OpenGL;
using Elffy.Exceptions;
using OpenTK.Graphics.OpenGL4;

namespace Elffy.Shading
{
    public sealed class PostProcessProgram : IDisposable
    {
        private PostProcess _source;
        private ProgramObject _program;
        private VBO _vbo;
        private IBO _ibo;
        private VAO _vao;

        internal PostProcessProgram(PostProcess source, in ProgramObject program, in VBO vbo, in IBO ibo, in VAO vao)
        {
            _source = source;
            _program = program;
            _vbo = vbo;
            _ibo = ibo;
            _vao = vao;
        }

        ~PostProcessProgram() => Dispose(false);

        public void Render(in Vector2i screenSize)
        {
            VAO.Bind(_vao);
            IBO.Bind(_ibo);
            ProgramObject.Bind(_program);
            _source.SendUniforms(_program, screenSize);
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
                ProgramObject.Delete(ref _program);
                VBO.Delete(ref _vbo);
                IBO.Delete(ref _ibo);
                VAO.Delete(ref _vao);
            }
            else {
                throw new MemoryLeakException(typeof(PostProcessProgram));
            }
        }
    }
}
