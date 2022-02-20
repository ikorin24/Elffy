#nullable enable
using Elffy.Graphics.OpenGL;
using OpenTK.Graphics.OpenGL4;

namespace Elffy.Shading
{
    /// <summary>
    /// Required: OpenGL 4.3
    /// </summary>
    public abstract class ComputeShaderSource
    {
        protected abstract string ShaderSource { get; }

        protected abstract void SendUniforms(Uniform uniform, ComputeShaderContext context);

        internal void Dispatch(ProgramObject program, IHostScreen screen, Vector3i groupCount)
        {
            var context = new ComputeShaderContext(screen);
            var uniform = new Uniform(program);
            ProgramObject.Bind(program);
            SendUniforms(uniform, context);
            GL.DispatchCompute(groupCount.X, groupCount.Y, groupCount.Z);
        }

        public ComputeShaderDispatcher Compile()
        {
            var screen = Engine.GetValidCurrentContext();
            var program = ShaderCompiler.CompileComputeShader(ShaderSource);
            return ComputeShaderDispatcher.Create(program, screen, this);
        }
    }
}
