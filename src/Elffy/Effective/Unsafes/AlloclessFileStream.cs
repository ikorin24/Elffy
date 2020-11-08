#nullable enable
using System;
using System.IO;
using System.Buffers;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Reflection;
using Elffy.AssemblyServices;

namespace Elffy.Effective.Unsafes
{
    // .NET Core 3.1 のソースをもとに作成
    // 
    // FileStream は Read, Write でバッファが必要になった時にバッファが null なら
    // _buffer = new byte[N]
    // で確保し、Dispose 後は放置されて使い捨てられます。
    // それを、このクラスでは ArrayPool<byte>.Shared から確保したバイトにすり替えて Dispose 時に返します。
    // バッファのフィールドは private なため、あらかじめ IL で組み立ててキャッシュしたデリゲード経由で
    // 配列を注入します。
    // 配列長はコンストラクタで指定した長さちょうどである必要があるため、
    // ArrayPool<byte>.Shared の仕様として 2^n でなくてはならない。


    /// <summary>内部バッファを使い捨てない <see cref="FileStream"/> クラス</summary>
    [CriticalDotnetDependency("netcoreapp3.1 || net5.0")]
    internal sealed class AlloclessFileStream : FileStream, IDisposable
    {
        const int BufferSize = 4096;    // 2^n でなければならない (ArrayPool<byte>.Shared からちょうどのサイズを取る必要があるため)
        private static readonly Action<FileStream, byte[]?> _setBufferDelegate;

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

            _setBufferDelegate = (Action<FileStream, byte[]?>)dm.CreateDelegate(typeof(Action<FileStream, byte[]?>));
        }

        private AlloclessFileStream(string path, FileMode mode, FileAccess access, FileShare share)
            : base(path, mode, access, share, BufferSize, useAsync: false)          // `useAsync` must be false
        {
            // Stream は長期間保持される可能性があるため、ArrayPool に返すまでの間に
            // 同じ配列長の Rent が発生する可能性があるが、
            // ArrayPool<T>.Shared はスレッドごとに独立なので発生頻度は低い上、
            // 二重構造で保持されるのでそうそう問題はないはず。やらないよりは全然マシ。

            // このクラスを使う側は Dispose を呼ぶまでの間に、BufferSize と同じ配列長になる配列を
            // 同じスレッド上で ArrayPool<byte> から借りないようにする必要がある。
            // 借りてもバグにはならないが、配列の使いまわしがうまく機能せずメリットが薄くなる。

            // また、ArrayPool<T>.Shared の性質上、このコンストラクタを呼んだスレッドと
            // 同じスレッドで Dispose を呼ばないとメモリのロスが発生します。
            // 別スレッドで Dispose したとしてもバグにはならないが、配列の使いまわしがうまく機能せず
            // 最大の効果を得られません。

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


                    // .net core 3.1 のソースを見る限り、FileStream の バッファは
                    // Dispose 時に特に何か処理をされることもなくそのまま放置されるが、
                    // 再現困難なバグを引き起こしても困るのでバッファは消しておく。
                    // 消しても null 参照が起こったりはしない。
                    _setBufferDelegate.Invoke(this, null);
                }
            }
            base.Dispose(disposing);
        }
    }
}
