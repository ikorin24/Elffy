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
using Elffy.Effective.Unsafes;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Elffy.Threading;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.OpenGL
{
    [Obsolete("未実装", true)]     // ファイルにプリコンパイルしたバイナリのロードはとりあえず未実装に
    internal static class ShaderPrecompileHelper
    {
        private const int GL_RROGRAM_BINARY_LENGTH = 0x8741;

        private static readonly string _cacheDirectory = Path.Combine(AssemblyState.EntryAssemblyDirectory, "cache", "glsl");
        public static Task CreateCacheFromProgramAsync(Type type, ProgramObject program)
        {
            if(type.IsSubclassOf(typeof(ShaderSource)) == false) {
                throw new ArgumentException();
            }
            if(program.IsEmpty) { throw new ArgumentException($"{nameof(program)} is empty"); }

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
        public static async Task<(ProgramObject, bool)> TryLoadProgramCacheAsync(Type type)
        {
            if(type.IsSubclassOf(typeof(ShaderSource)) == false) {
                throw new ArgumentException();
            }

            // TODO: 例外発生時のメモリ開放が甘い気がする。たぶんリークする。要修正

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
                    return (program, !program.IsEmpty);
                }
            }
            else {
                Debug.Assert(binary.IsEmpty);
                return (default, false);
            }
        }

        private static ProgramObject CreateProgram(BinaryFormat format, ReadOnlySpan<byte> binary)
        {
            var program = ProgramObject.Create();

            GL.ProgramBinary(program.Value, format,
                             ref MemoryMarshal.GetReference(binary),
                             binary.Length);
            GL.GetProgram(program.Value, GetProgramParameterName.LinkStatus, out int linkStatus);
            if(linkStatus == Consts.ShaderProgramLinkFailed) {
                ProgramObject.Delete(ref program);
                Debug.Assert(program.IsEmpty);
            }
            return program;
        }


        private static unsafe (BinaryFormat, ValueTypeRentMemory<byte>) GetBinary(ProgramObject program)
        {
            ValueTypeRentMemory<byte> binary = default;
            try {
                GL.GetProgram(program.Value, (GetProgramParameterName)GL_RROGRAM_BINARY_LENGTH, out int binLen);
                binary = new ValueTypeRentMemory<byte>(binLen);
                GL.GetProgramBinary(program.Value, binLen, out int len, out var f, ref MemoryMarshal.GetReference(binary.Span));
                Debug.Assert(binLen == len);

                return (f, binary);
            }
            catch(Exception ex) {
                binary.Dispose();
                throw new InvalidOperationException("Some exception thrown. (See inner exception)", ex);
            }
        }
    }

    internal static class ShaderProgramOnMemoryManager
    {
        public static readonly Dictionary<Type, int> RefCountTable = new Dictionary<Type, int>();
    }
}
