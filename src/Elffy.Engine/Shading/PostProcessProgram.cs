﻿#nullable enable
using System;
using Elffy.Graphics.OpenGL;
using Elffy.Features;
using OpenTK.Graphics.OpenGL4;

namespace Elffy.Shading
{
    /// <summary>Compiled post process program</summary>
    internal sealed class PostProcessProgram : IDisposable
    {
        private PostProcess _source;
        private ProgramObject _program;
        private VBO _vbo;
        private IBO _ibo;
        private VAO _vao;

        internal PostProcessProgram(PostProcess source, in ProgramObject program, in VBO vbo, in IBO ibo, in VAO vao, IHostScreen associatedScreen)
        {
            _source = source;
            _program = program;
            _vbo = vbo;
            _ibo = ibo;
            _vao = vao;
            ContextAssociatedMemorySafety.Register(this, associatedScreen);
        }

        ~PostProcessProgram() => Dispose(false);

        public void Render(in PostProcessRenderContext context) => Render(in context, Vector2.One);

        public void Render(in PostProcessRenderContext context, in Vector2 uvScale)
        {
            VAO.Bind(_vao);
            IBO.Bind(_ibo);
            ProgramObject.UseProgram(_program);
            _source.OnRenderingInternal(_program, in context, in uvScale);
            GL.DrawElements(BeginMode.Triangles, (int)_ibo.Length, DrawElementsType.UnsignedInt, 0);
            VAO.Unbind();
            IBO.Unbind();
        }

        /// <summary>Dispose post process program</summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if(_program.IsEmpty) { return; }
            if(disposing) {
                ProgramObject.Delete(ref _program);
                VBO.Delete(ref _vbo);
                IBO.Delete(ref _ibo);
                VAO.Delete(ref _vao);
            }
            else {
                ContextAssociatedMemorySafety.OnFinalized(this);
            }
        }
    }
}
