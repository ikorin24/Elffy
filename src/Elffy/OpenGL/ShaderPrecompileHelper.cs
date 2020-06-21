#nullable enable
using OpenToolkit.Graphics.OpenGL;
using System;
using Elffy.Effective;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elffy.Core;
using System.IO;
using System.Threading.Tasks;
using Elffy.Shading;
using Elffy.AssemblyServices;
using Elffy.Effective.Internal;
using System.Runtime.InteropServices;

namespace Elffy.OpenGL
{
    internal static class ShaderPrecompileHelper
    {
        private const int GL_RROGRAM_BINARY_LENGTH = 0x8741;

        private static readonly string _cacheDirectory = Path.Combine(AssemblyState.EntryAssemblyDirectory, "cache", "glsl");
        //private static readonly Hashtable _table = new Hashtable();

        public static Task CreateCacheFromProgramAsync(Type type, int program)
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

        /// <summary>
        /// 指定の <see cref="ShaderSource"/> のキャッシュを取得します。(このメソッドは内部でスレッド復帰しています)
        /// </summary>
        /// キャッシュを取得する <param name="type"><see cref="ShaderSource"/> の派生型</param>
        /// <returns>(program, success) のペア</returns>
        public static async Task<(int, bool)> TryLoadProgramCacheAsync(Type type)
        {
            if(type.IsSubclassOf(typeof(ShaderSource)) == false) {
                throw new ArgumentException();
            }

            var (format, binary, success) = await Task.Factory.StartNew(state =>
            {
                Debug.Assert(state is Type);
                var type = Unsafe.As<Type>(state);

                var cachePath = Path.Combine(_cacheDirectory, type.FullName!);
                if(!File.Exists(cachePath)) {
                    return (default, default, false);
                }
                using(var stream = AlloclessFileStream.OpenRead(cachePath)) {
                    ValueTypeRentMemory<byte> binary = default;
                    Span<byte> buf = stackalloc byte[sizeof(int) * 2];
                    if(stream.Read(buf) != buf.Length) {
                        goto ERROR;
                    }
                    var format = (BinaryFormat)BitConverter.ToInt32(buf);
                    var binLen = BitConverter.ToInt32(buf.Slice(sizeof(int)));
                    binary = new ValueTypeRentMemory<byte>(binLen);
                    var binarySpan = binary.Span;
                    if(stream.Read(binarySpan) != binarySpan.Length) {
                        goto ERROR;
                    }
                    return (format, binary, true);


                ERROR:
                    binary.Dispose();
                    binary = default;
                    return (default, default, false);
                }
            }, type).ConfigureAwait(true);

            if(success) {
                Debug.Assert(!binary.IsEmpty);
                using(binary) {
                    var program = CreateProgram(format, binary.Span);
                    return (program == Consts.NULL) ? (default, false) : (program, true);
                }
            }
            else {
                Debug.Assert(binary.IsEmpty);
                return (default, false);
            }
        }

        private static int CreateProgram(BinaryFormat format, ReadOnlySpan<byte> binary)
        {
            var program = GL.CreateProgram();
            GL.ProgramBinary(program, format,
                             ref MemoryMarshal.GetReference(binary),
                             binary.Length);
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int linkStatus);
            if(linkStatus == Consts.ShaderProgramLinkFailed) {
                GL.DeleteProgram(program);
                program = Consts.NULL;
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
    }
}
