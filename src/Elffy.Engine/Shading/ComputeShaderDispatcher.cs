#nullable enable
using System;
using System.Diagnostics;
using Elffy.Features;
using Elffy.Graphics.OpenGL;

namespace Elffy.Shading
{
    public sealed class ComputeShaderDispatcher : IDisposable
    {
        private readonly IComputeShader _shader;
        private readonly IHostScreen _screen;
        private ProgramObject _program;

        public IHostScreen Screen => _screen;

        private ComputeShaderDispatcher(ProgramObject program, IHostScreen screen, IComputeShader shader)
        {
            Debug.Assert(program.IsEmpty == false);
            _program = program;
            _screen = screen;
            _shader = shader;
        }

        ~ComputeShaderDispatcher() => Dispose(false);

        public static ComputeShaderDispatcher Create(IComputeShader shader)
        {
            ArgumentNullException.ThrowIfNull(shader);

            var screen = Engine.GetValidCurrentContext();
            var source = IComputeShader.GetShaderSourceInternal(shader);
            var program = ShaderCompiler.CompileComputeShader(source);
            var instance = new ComputeShaderDispatcher(program, screen, shader);
            ContextAssociatedMemorySafety.Register(instance, screen);
            return instance;
        }

        public void Dispatch(int xGroupCount, int yGroupCount, int zGroupCount)
        {
            var screen = _screen;
            ContextMismatchException.ThrowIfContextNotEqual(Engine.GetValidCurrentContext(), screen);

            var program = _program;
            ProgramObject.Bind(program);
            var context = new ComputeShaderContext(screen);
            var uniform = new ShaderDataDispatcher(program);
            IComputeShader.SendUniformsInternal(_shader, uniform, context);
            IComputeShader.DispatchCompute(xGroupCount, yGroupCount, zGroupCount);
        }

        public void Dispatch(Vector3i groupCount)
        {
            Dispatch(groupCount.X, groupCount.Y, groupCount.Z);
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
