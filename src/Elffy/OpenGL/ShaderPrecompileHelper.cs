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
using System.Collections;
using Elffy.Shading;
using System.Reflection;
using Elffy.AssemblyServices;
using Elffy.Effective.Internal;
using System.Runtime.InteropServices;

namespace Elffy.OpenGL
{
    internal static class ShaderPrecompileHelper
    {
        private const int GL_RROGRAM_BINARY_LENGTH = 0x8741;
        //private static int[]? _formats;

        private static readonly string _cacheDirectory = Path.Combine(AssemblyState.EntryAssemblyDirectory, "cache", "glsl");
        //private static readonly Hashtable _table = new Hashtable();

        public static Task CreateCacheAsync(Type type, int program)
        {
            if(type.IsSubclassOf(typeof(ShaderSource)) == false) {
                throw new ArgumentException();
            }

            var (f, binary) = GetBinary(program);
            return Task.Factory.StartNew(state =>
            {
                Debug.Assert(state is Type);
                var type = Unsafe.As<Type>(state);

                Directory.CreateDirectory(_cacheDirectory);
                var cachePath = Path.Combine(_cacheDirectory, type.FullName!);

                using(var stream = AlloclessFileStream.OpenWrite(cachePath)) {
                    Span<byte> buf = stackalloc byte[sizeof(int)];
                    ref int intBuf = ref Unsafe.As<byte, int>(ref MemoryMarshal.GetReference(buf));
                    intBuf = (int)f;
                    stream.Write(buf);
                    intBuf = binary.Span.Length;
                    stream.Write(buf);
                    stream.Write(binary.Span);
                }
            }, type);
        }


        public static async Task<int> LoadCacheAsync(Type type)
        {
            if(type.IsSubclassOf(typeof(ShaderSource)) == false) {
                throw new ArgumentException();
            }

            var (format, binary) = await Task.Factory.StartNew(state =>
            {
                Debug.Assert(state is Type);
                var type = Unsafe.As<Type>(state);

                var cachePath = Path.Combine(_cacheDirectory, type.FullName!);

                using(var stream = AlloclessFileStream.OpenRead(cachePath)) {
                    Span<byte> buf = stackalloc byte[sizeof(int) * 2];
                    if(stream.Read(buf) != buf.Length) {
                        throw new Exception();
                    }
                    var format = (BinaryFormat)BitConverter.ToInt32(buf);
                    var binLen = BitConverter.ToInt32(buf.Slice(sizeof(int)));
                    var binary = new ValueTypeRentMemory<byte>(binLen);

                    var binarySpan = binary.Span;
                    if(stream.Read(binarySpan) != binarySpan.Length) {
                        throw new Exception();
                    }

                    return (format, binary);
                }
            }, type).ConfigureAwait(true);


            return CreateProgram(format, binary);
        }

        private static int CreateProgram(BinaryFormat format, ValueTypeRentMemory<byte> binary)
        {
            var program = GL.CreateProgram();
            var binarySpan = binary.Span;
            GL.ProgramBinary(program, format,
                             ref MemoryMarshal.GetReference(binarySpan),
                             binarySpan.Length);
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int linkStatus);
            if(linkStatus == Consts.ShaderProgramLinkFailed) {
                var log = GL.GetProgramInfoLog(program);
                throw new InvalidOperationException($"Linking shader is failed.{Environment.NewLine}{log}");
            }
            return program;
        }


        private static unsafe (BinaryFormat, ValueTypeRentMemory<byte>) GetBinary(int program)
        {
            ValueTypeRentMemory<byte> binary = default;
            try {
                GL.GetProgram(program, (GetProgramParameterName)GL_RROGRAM_BINARY_LENGTH, out int binLen);
                binary = new ValueTypeRentMemory<byte>(binLen);
                GL.GetProgramBinary(program, binLen, out int len, out var f, ref MemoryMarshal.GetReference(binary.Span));
                Debug.Assert(binLen == len);

                return (f, binary);
            }
            catch(Exception ex) {
                binary.Dispose();
                throw new InvalidOperationException("Some exception thrown. (See inner exception)", ex);
            }
        }

        //public static unsafe bool TryCreateFromBinary(Span<byte> binary, Span<int> formats, out int shaderProgram)
        //{
        //    if(!GetFormats().AsSpan().SequenceEqual(formats)) {
        //        shaderProgram = 0;
        //        return false;
        //    }
        //    shaderProgram = GL.CreateProgram();
        //    GL.ProgramBinary(shaderProgram,
        //                     Unsafe.As<int, BinaryFormat>(ref formats.GetPinnableReference()),
        //                     ref binary.GetPinnableReference(),
        //                     binary.Length);
        //    int linkStatus = 0;
        //    GL.GetProgram(shaderProgram, GetProgramParameterName.LinkStatus, &linkStatus);
        //    if(linkStatus == Consts.ShaderProgramLinkFailed) {
        //        //var log = GL.GetProgramInfoLog(shaderProgram);
        //        //throw new InvalidOperationException($"Linking shader is failed.{Environment.NewLine}{log}");
        //        shaderProgram = 0;
        //        return false;
        //    }

        //    return true;
        //}

        //public static ReadOnlyMemory<int> GetFormats()
        //{
        //    if(_formats is null == false) { return _formats; }
        //    GL.GetInteger(GetPName.NumProgramBinaryFormats, out int formatsLen);
        //    var format = new int[formatsLen];
        //    GL.GetInteger(GetPName.ProgramBinaryFormats, format);
        //    return (_formats = format);
        //}
    }
}
