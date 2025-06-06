﻿#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Elffy.Effective;
using Elffy.Effective.Unsafes;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Elffy.Graphics.OpenGL
{
    internal static class ShaderCompiler
    {
        public static ProgramObject CompileComputeShader(ReadOnlySpan<byte> computeShaderSource)
        {
            int shader = CompileShader(computeShaderSource, ShaderType.ComputeShader);
            try {
                return LinkShaders(new ReadOnlySpan<int>(in shader));
            }
            finally {
                if(shader != 0) {
                    GL.DeleteShader(shader);
                }
            }
        }

        public static ProgramObject Compile(ReadOnlySpan<byte> vertexShader, ReadOnlySpan<byte> fragmentShader)
            => Compile(vertexShader, fragmentShader, ReadOnlySpan<byte>.Empty);

        public static ProgramObject Compile(ReadOnlySpan<byte> vertexShader, ReadOnlySpan<byte> fragmentShader, ReadOnlySpan<byte> geometryShader)
        {
            if(vertexShader.IsEmpty) {
                throw new ArgumentException(nameof(vertexShader));
            }
            if(fragmentShader.IsEmpty) {
                throw new ArgumentException(nameof(fragmentShader));
            }
            Span<int> shaders = stackalloc int[3] { 0, 0, 0 };
            try {
                shaders[0] = CompileShader(vertexShader, ShaderType.VertexShader);
                shaders[1] = CompileShader(fragmentShader, ShaderType.FragmentShader);
                if(geometryShader.IsEmpty == false) {
                    shaders[2] = CompileShader(geometryShader, ShaderType.GeometryShader);
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


        private static int CompileShader(ReadOnlySpan<byte> shaderSource, ShaderType shaderType)
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

        private static void ThrowIfCompilationFailure(int shaderID, ReadOnlySpan<byte> source, int compilationStatus)
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
        private static void ThrowCompilationFailure(int shaderID, ReadOnlySpan<byte> source)
        {
            var sourceString = Encoding.UTF8.GetString(source);

            var log = GL.GetShaderInfoLog(shaderID);
            var sb = new StringBuilder();
            sb.AppendLine("Failed to compile shaders.");
            sb.AppendLine(log);
            var lines = sourceString.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
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

        private unsafe static class GLHelper
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
                var buflen = utf8.GetMaxByteCount(source.Length);
                using var buf = new UnsafeRawArray<byte>(buflen, false, out var span);
                var len = utf8.GetBytes(source, span);
                ShaderSource(shader, span[..len]);
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
    }

    public sealed class GlslException : Exception
    {
        public GlslException(string? message) : base(message)
        {
        }
    }
}
