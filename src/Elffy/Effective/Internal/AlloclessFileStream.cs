#nullable enable
using System;
using System.IO;
using System.Buffers;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Reflection;

namespace Elffy.Effective.Internal
{
    internal sealed class AlloclessFileStream : FileStream, IDisposable
    {
        const int BufferSize = 4096;    // 2^n でなければならない (ArrayPool<byte>.Shared からちょうどのサイズを取る必要があるため)
        private static readonly Action<FileStream, byte[]> _setBufferDelegate;

        private byte[]? _pooled;

        static AlloclessFileStream()
        {
            var dm = new DynamicMethod("SetBufferDynamic",
                                       MethodAttributes.Public | MethodAttributes.Static,
                                       CallingConventions.Standard,
                                       typeof(void),
                                       new[] { typeof(FileStream), typeof(byte[]), },
                                       typeof(AlloclessFileStream).Module,
                                       true);

            var bufferField = typeof(FileStream).GetField("_buffer", BindingFlags.Instance | BindingFlags.NonPublic);
            Debug.Assert(bufferField is null == false);

            const int ILStreamSize = 8;
            var il = dm.GetILGenerator(ILStreamSize);
            il.Emit(OpCodes.Ldarg_0);   // FileStrem
            il.Emit(OpCodes.Ldarg_1);   // byte[]
            il.Emit(OpCodes.Stfld, bufferField!);
            il.Emit(OpCodes.Ret);

            // ↑ -----------------------------------------------------------------
            // static SetBufferDynamic(FileStream stream, byte[] buf)
            // {
            //     stream._buffer = buf;
            // }
            // --------------------------------------------------------------------
            Debug.Assert(il.ILOffset <= ILStreamSize);

            _setBufferDelegate = (Action<FileStream, byte[]>)dm.CreateDelegate(typeof(Action<FileStream, byte[]>));
        }

        private AlloclessFileStream(string path, FileMode mode, FileAccess access, FileShare share)
            : base(path, mode, access, share, BufferSize, useAsync: false)          // `useAsync` must be false
        {
            // Stream は長期間保持される可能性があるため、ArrayPool に返すまでの間に
            // 同じ配列長の Rent が発生する可能性があるが、
            // ArrayPool<T>.Shared はスレッドごとに独立なので発生頻度は低い上、
            // 二重構造で保持されるのでそうそう問題はないはず。やらないよりは全然マシ。

            _pooled = ArrayPool<byte>.Shared.Rent(BufferSize);
            Debug.Assert(_pooled.Length == BufferSize);
            _setBufferDelegate.Invoke(this, _pooled);
        }

        ~AlloclessFileStream() => Dispose(false);


        public static AlloclessFileStream OpenRead(string path)
        {
            return new AlloclessFileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public static AlloclessFileStream OpenWrite(string path)
        {
            if(File.Exists(path)) {
                File.Delete(path);
            }
            return new AlloclessFileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);
        }

        protected override void Dispose(bool disposing)
        {
            if(disposing) {
                var pooled = _pooled;
                if(pooled is null == false) {
                    ArrayPool<byte>.Shared.Return(pooled);
                    _pooled = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
