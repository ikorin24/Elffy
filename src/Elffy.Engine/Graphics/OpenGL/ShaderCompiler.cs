#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Elffy.Effective;
using Elffy.Effective.Unsafes;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;

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
                GLHelper.ShaderSource((uint)shaderID, shaderSource);
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

    internal unsafe static class GLHelper
    {
        private static delegate* unmanaged[Stdcall]<uint, int, byte**, int*, void> _glShaderSource;

        public static void ShaderSource(uint shader, ReadOnlySpan<byte> source)
        {
            var len = source.Length;
            fixed(byte* s = source) {
                glShaderSource(shader, 1, &s, &len);
            }
        }

        public static void ShaderSource(uint shader, ReadOnlySpan<char> source)
        {
            var utf8 = Encoding.UTF8;
            var len = utf8.GetByteCount(source);
            using var b = new UnsafeRawArray<byte>(len, false);
            var byteSpan = b.AsSpan();
            utf8.GetBytes(source, byteSpan);
            ShaderSource(shader, byteSpan);
        }

        private static void glShaderSource(uint shader, int sourceCount, byte** sources, int* lengths)
        {
            if(_glShaderSource == null) {
                _glShaderSource = (delegate* unmanaged[Stdcall]<uint, int, byte**, int*, void>)GLFW.GetProcAddressRaw("glShaderSource"u8.AsPointer());
            }

            // [opengl specification]
            // If 'lengths' is null, each string is assumed to be null terminated.
            // If 'lengths' is a value other than null, it points to an array containing a string length for each of the corresponding elements of string.
            _glShaderSource(shader, sourceCount, sources, lengths);
        }
    }

    public sealed class GlslException : Exception
    {
        public GlslException(string? message) : base(message)
        {
        }
    }
}
