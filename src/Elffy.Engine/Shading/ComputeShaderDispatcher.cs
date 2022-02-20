#nullable enable
using System;
using System.Diagnostics;
using Elffy.Features;
using Elffy.Graphics.OpenGL;

namespace Elffy.Shading
{
    public sealed class ComputeShaderDispatcher : IDisposable
    {
        private readonly ComputeShader _source;
        private readonly IHostScreen _screen;
        private ProgramObject _program;

        private ComputeShaderDispatcher(ProgramObject program, IHostScreen screen, ComputeShader source)
        {
            Debug.Assert(program.IsEmpty == false);
            _program = program;
            _screen = screen;
            _source = source;
        }

        ~ComputeShaderDispatcher() => Dispose(false);

        internal static ComputeShaderDispatcher Create(ProgramObject program, IHostScreen screen, ComputeShader source)
        {
            var instance = new ComputeShaderDispatcher(program, screen, source);
            ContextAssociatedMemorySafety.Register(instance, screen);
            return instance;
        }

        public void Dispatch(int xGroupCount, int yGroupCount, int zGroupCount)
        {
            Dispatch(new Vector3i(xGroupCount, yGroupCount, zGroupCount));
        }

        public void Dispatch(Vector3i groupCount)
        {
            var screen = _screen;
            ContextMismatchException.ThrowIfContextNotEqual(Engine.GetValidCurrentContext(), screen);
            _source.Dispatch(_program, screen, groupCount);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if(disposing) {
                ContextMismatchException.ThrowIfContextNotEqual(Engine.GetValidCurrentContext(), _screen);
                ProgramObject.Delete(ref _program);
            }
            else {
                ContextAssociatedMemorySafety.OnFinalized(this);
            }
        }
    }
}
