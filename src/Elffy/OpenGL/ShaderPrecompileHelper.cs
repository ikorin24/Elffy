#nullable enable
using OpenToolkit.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Text;
using Elffy.Effective;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elffy.Core;
using System.IO;
using System.Threading.Tasks;

namespace Elffy.OpenGL
{
    internal static class ShaderPrecompileHelper
    {
        private const int GL_RROGRAM_BINARY_LENGTH = 0x8741;
        private static int[]? _formats;

        public static unsafe void GetBinary(int program, out ValueTypeRentMemory<byte> binary, out ReadOnlyMemory<int> formats)
        {
            formats = GetFormats();
            binary = default;
            try {
                GL.GetProgram(program, (GetProgramParameterName)GL_RROGRAM_BINARY_LENGTH, out int binLen);
                binary = new ValueTypeRentMemory<byte>(binLen);
                GL.GetProgramBinary(program, binLen, out int len, out var f, ref binary.Span.GetPinnableReference());
                Debug.Assert(binLen == len);
            }
            catch(Exception ex) {
                binary.Dispose();
                binary = default;
                throw new InvalidOperationException("Some exception thrown. (See inner exception)", ex);
            }
        }

        public static unsafe bool TryCreateFromBinary(Span<byte> binary, Span<int> formats, out int shaderProgram)
        {
            if(!GetFormats().AsSpan().SequenceEqual(formats)) {
                shaderProgram = 0;
                return false;
            }
            shaderProgram = GL.CreateProgram();
            GL.ProgramBinary(shaderProgram,
                             Unsafe.As<int, BinaryFormat>(ref formats.GetPinnableReference()),
                             ref binary.GetPinnableReference(),
                             binary.Length);
            int linkStatus = 0;
            GL.GetProgram(shaderProgram, GetProgramParameterName.LinkStatus, &linkStatus);
            if(linkStatus == Consts.ShaderProgramLinkFailed) {
                //var log = GL.GetProgramInfoLog(shaderProgram);
                //throw new InvalidOperationException($"Linking shader is failed.{Environment.NewLine}{log}");
                shaderProgram = 0;
                return false;
            }

            return true;
        }

        private static int[] GetFormats()
        {
            if(_formats is null == false) { return _formats; }
            GL.GetInteger(GetPName.NumProgramBinaryFormats, out int formatsLen);
            var format = new int[formatsLen];
            GL.GetInteger(GetPName.ProgramBinaryFormats, format);
            return (_formats = format);
        }
    }
}
