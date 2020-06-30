#nullable enable
using System;
using System.Runtime.CompilerServices;
using Elffy.Exceptions;
using Elffy.Core;
using Elffy.OpenGL;
using System.Diagnostics;

namespace Elffy.Shading
{
    public sealed class ShaderProgram : IDisposable
    {
        private ProgramObject _program;

        private ShaderSource _shaderSource;
        private bool _initialized;

        internal bool IsReleased => _program.IsEmpty;

        internal ShaderProgram(ShaderSource shaderSource, ProgramObject program)
        {
            if(shaderSource is null) { throw new ArgumentNullException(nameof(shaderSource)); }
            Debug.Assert(program.IsEmpty == false);
            _program = program;
            _shaderSource = shaderSource;
        }

        ~ShaderProgram() => Dispose(false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Apply(Renderable target, ReadOnlySpan<Light> lights, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            if(IsReleased) { throw new InvalidOperationException("this shader program is empty or deleted."); }
            if(!_initialized) { throw new InvalidOperationException("The shader is not initialized."); }
            ProgramObject.Bind(_program);
            _shaderSource!.SendUniforms(_program, target, lights, model, view, projection);
        }

        internal void Initialize(in VAO vao, in VBO vbo)
        {
            VAO.Bind(vao);
            VBO.Bind(vbo);
            _shaderSource!.DefineLocation(_program);
            _initialized = true;
            VAO.Unbind();
            VBO.Unbind();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if(IsReleased) { return; }
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Dispose(bool disposing)
        {
            if(disposing) {
                ProgramObject.Delete(ref _program);
            }
            else {
                // Can not release resources because finalizer is called from another thread.
                throw new MemoryLeakException(GetType());
            }
        }
    }
}
