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
            SendUniforms(uniform, context);
            GL.DispatchCompute(groupCount.X, groupCount.Y, groupCount.Z);
        }

        public ComputeShaderDispatcher Compile()
        {
            var screen = Engine.GetValidCurrentContext();
            var program = CompilePrivate(this);
            return ComputeShaderDispatcher.Create(program, screen, this);
        }

        private static ProgramObject CompilePrivate(ComputeShaderSource source)
        {
            var shaderSource = source.ShaderSource;
            var program = ProgramObject.Empty;
            int shaderID = Consts.NULL;
            try {
                shaderID = GL.CreateShader(ShaderType.ComputeShader);
                GL.ShaderSource(shaderID, shaderSource);
                GL.CompileShader(shaderID);
                GL.GetShader(shaderID, ShaderParameter.CompileStatus, out int compilationStatus);
                ShaderCompilationHelper.ThrowIfCompilationFailure(shaderID, shaderSource, compilationStatus);

                program = ProgramObject.Create();
                GL.AttachShader(program.Value, shaderID);
                GL.LinkProgram(program.Value);
                GL.GetProgram(program.Value, GetProgramParameterName.LinkStatus, out int linkStatus);
                ShaderCompilationHelper.ThrowIfLinkFailed(program.Value, linkStatus);
                return program;
            }
            catch {
                ProgramObject.Delete(ref program);
                throw;
            }
            finally {
                if(shaderID != Consts.NULL) {
                    GL.DeleteShader(shaderID);
                }
            }
        }
    }
}
