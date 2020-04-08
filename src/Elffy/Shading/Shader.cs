#nullable enable
using System;
using System.Runtime.CompilerServices;
using Elffy.Exceptions;
using Elffy.Core;
using OpenTK.Graphics.OpenGL;

namespace Elffy.Shading
{
    public sealed class Shader : IDisposable
    {
        private int _program = Consts.NULL;
        private ShaderSource? _shaderSource;

        private bool IsReleased => _program == Consts.NULL;

        private static int _currentProgram = Consts.NULL;

        internal Shader(ShaderSource shaderSource, int program)
        {
            ArgumentChecker.ThrowIfNullArg(shaderSource, nameof(shaderSource));
            ArgumentChecker.ThrowArgumentIf(program == Consts.NULL, "invalid shader program object");
            _program = program;
            _shaderSource = shaderSource;
        }

        ~Shader() => Dispose(false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Apply(Renderable target, in Matrix4 model, in Matrix4 view, in Matrix4 projection)
        {
            if(IsReleased) { throw new InvalidOperationException("this shader program is empty or deleted."); }
            if(_currentProgram != _program) {
                _currentProgram = _program;
                GL.UseProgram(_program);
            }
            _shaderSource!.SendUniformsInternal(_program, target, model, view, projection);
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
                if(IsReleased) { return; }
                GL.DeleteProgram(_program);
                _program = Consts.NULL;
                _shaderSource = null;
            }
            else {
                // Can not release resources because finalizer is called from another thread.
                throw new MemoryLeakException(GetType());
            }
        }
    }
}
