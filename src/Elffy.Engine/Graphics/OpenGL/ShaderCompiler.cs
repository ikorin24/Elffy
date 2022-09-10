#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using OpenTK.Graphics.OpenGL4;

namespace Elffy.Graphics.OpenGL
{
    internal static class ShaderCompiler
    {
        public static ProgramObject CompileComputeShader(string computeShaderSource)
        {
            int shader = CompileShader(computeShaderSource, ShaderType.ComputeShader);
            try {
                return LinkShaders(stackalloc int[1] { shader });
            }
            finally {
                if(shader != 0) {
                    GL.DeleteShader(shader);
                }
            }
        }

        public static ProgramObject Compile([AllowNull] string vertexShaderSource, [AllowNull] string fragmentShaderSource, string? geometryShaderSource)
        {
            ArgumentNullException.ThrowIfNull(vertexShaderSource);
            ArgumentNullException.ThrowIfNull(fragmentShaderSource);
            Span<int> shaders = stackalloc int[3] { 0, 0, 0 };
            try {
                shaders[0] = CompileShader(vertexShaderSource, ShaderType.VertexShader);
                shaders[1] = CompileShader(fragmentShaderSource, ShaderType.FragmentShader);
                if(geometryShaderSource != null) {
                    shaders[2] = CompileShader(geometryShaderSource, ShaderType.GeometryShader);
                }
                return LinkShaders(shaders);
            }
            finally {
                foreach(var shader in shaders) {
                    if(shader != 0) {
                        GL.DeleteShader(shader);
                    }
                }
            }
        }

        private static int CompileShader(string shaderSource, ShaderType shaderType)
        {
            int shaderID = 0;
            try {
                shaderID = GL.CreateShader(shaderType);
                GL.ShaderSource(shaderID, shaderSource);
                GL.CompileShader(shaderID);
                GL.GetShader(shaderID, ShaderParameter.CompileStatus, out int compilationStatus);
                ThrowIfCompilationFailure(shaderID, shaderSource, compilationStatus);
                return shaderID;
            }
            catch {
                if(shaderID != 0) {
                    GL.DeleteShader(shaderID);
                }
                throw;
            }
        }

        private static ProgramObject LinkShaders(ReadOnlySpan<int> shaders)
        {
            var program = ProgramObject.Empty;
            try {
                program = ProgramObject.Create();
                foreach(var shader in shaders) {
                    if(shader != 0) {
                        GL.AttachShader(program.Value, shader);
                    }
                }
                GL.LinkProgram(program.Value);
                GL.GetProgram(program.Value, GetProgramParameterName.LinkStatus, out int linkStatus);
                ThrowIfLinkFailed(program.Value, linkStatus);
                return program;
            }
            catch {
                ProgramObject.Delete(ref program);
                throw;
            }
        }

        private static void ThrowIfCompilationFailure(int shaderID, string source, int compilationStatus)
        {
            const int Failure = 0;
            if(compilationStatus == Failure) {
                ThrowCompilationFailure(shaderID, source);
            }
        }

        private static void ThrowIfLinkFailed(int programID, int linkStatus)
        {
            const int Failure = 0;
            if(linkStatus == Failure) {
                ThrowLinkFailed(programID);
            }
        }

        [DoesNotReturn]
        private static void ThrowCompilationFailure(int shaderID, string source)
        {
            var log = GL.GetShaderInfoLog(shaderID);
            var sb = new StringBuilder();
            sb.AppendLine("Failed to compile shaders.");
            sb.AppendLine(log);
            var lines = source.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for(int l = 0; l < lines.Length; l++) {
                sb.Append(string.Format("{0, -4}\t", l + 1));
                sb.AppendLine(lines[l]);
            }
            throw new GlslException(sb.ToString());
        }

        [DoesNotReturn]
        private static void ThrowLinkFailed(int programID)
        {
            var log = GL.GetProgramInfoLog(programID);
            throw new GlslException($"Failed to link shaders.\n{log}");
        }
    }

    public sealed class GlslException : Exception
    {
        public GlslException(string? message) : base(message)
        {
        }
    }
}
