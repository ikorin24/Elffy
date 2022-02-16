#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Elffy.Graphics.OpenGL;
using OpenTK.Graphics.OpenGL4;

namespace Elffy.Shading
{
    internal static class ShaderCompilationHelper
    {
        public static void ThrowIfCompilationFailure(int shaderID, string source, int compilationStatus)
        {
            if(compilationStatus == Consts.ShaderCompileFailed) {
                ThrowCompilationFailure(shaderID, source);
            }
        }

        public static void ThrowIfLinkFailed(int programID, int linkStatus)
        {
            if(linkStatus == Consts.ShaderProgramLinkFailed) {
                ThrowLinkFailed(programID);
            }
        }

        [DoesNotReturn]
        private static void ThrowCompilationFailure(int shaderID, string source)
        {
            var log = GL.GetShaderInfoLog(shaderID);
            var sb = new StringBuilder();
            sb.AppendLine("Compiling shader is Failed.");
            sb.AppendLine(log);
            var lines = source.Split('\n');
            for(int l = 0; l < lines.Length; l++) {
                sb.Append(string.Format("{0, 3}\t", l + 1));
                sb.Append(lines[l]);
                sb.Append('\n');
            }
            throw new InvalidDataException(sb.ToString());
        }

        [DoesNotReturn]
        private static void ThrowLinkFailed(int programID)
        {
            var log = GL.GetProgramInfoLog(programID);
            throw new InvalidOperationException($"Linking shader is failed.\n{log}");
        }
    }
}
