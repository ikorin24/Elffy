#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy.Effective
{
    // TODO: 確実にメモリリークをしない方法を考える
    // 可能な限りガベージの発生を避けたいので struct 実装にしたが、class と違いファイナライザが使えないため
    // Dispose を呼び忘れるといろいろと困る。
    // 特にアンマネージドメモリを確保した場合はメモリリークするので、何かしらの対策を考える。
    // マネージドメモリの場合でも2回 Dispose されると色々と困る。
    // 内部実装だけ class にしてインスタンスをプールして使いまわすか、struct をやめるかのどちらかが妥当。
    // ベンチマークをとるべき

    /// <summary>Shared memories from memory pool, that provides <see cref="Span{T}"/> like <see cref="Memory{T}"/>.</summary>
    /// <typeparam name="T">element type</typeparam>
    [DebuggerDisplay("{DebugDisplay}")]
    public readonly struct ValueTypeRentMemory<T> : IEquatable<ValueTypeRentMemory<T>>, IDisposable where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebugDisplay => $"{nameof(ValueTypeRentMemory<T>)}<{typeof(T).Name}>[{Span.Length}]";

        // IMemoryOwner<T> を継承するメリットが特になく、
        // Memory<T> を公開する方法もないので
        // IMemoryOwner<T> は継承しない。

        // [メモリを借りてきたとき]
        // _array : 借りた配列
        // _start : 使用可能なメモリの開始位置が _array[(int)_start]
        // _byteLength : 使用可能なメモリのバイト長
        // _id    : 借りたメモリの識別用番号 (>= 0)
        // _lender : メモリの貸し出し者  (>= 0)
        //
        // [unmanaged メモリを確保した時]
        // _array : null
        // _start : unmanaged メモリのポインタ
        // _byteLength : 使用可能なメモリのバイト長
        // _id    : -1
        // _lender : -1

        private readonly byte[]? _array;
        private readonly IntPtr _start;
        private readonly int _byteLength;
        private readonly int _id;
        private readonly int _lender;

        public static ValueTypeRentMemory<T> Empty => default;

        public unsafe readonly Span<T> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _array is null ? new Span<T>((void*)_start, _byteLength / sizeof(T))
                                  : MemoryMarshal.Cast<byte, T>(_array.AsSpan(_start.ToInt32(), _byteLength));
        }

        // default(ValueTypeRentMemory<T>) は _id と _lender が0ですが、プールから借りたメモリではない。
        // _byteLength が0かどうかで、Empty を判断します。

        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _byteLength == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ValueTypeRentMemory(int length)
        {
            if(length == 0) {
                this = default;
                return;
            }
            if(MemoryPool.TryRentByteMemory<T>(length, out _array, out int start, out _byteLength, out _id, out _lender)) {
                Debug.Assert(_array is null == false);
                _start = new IntPtr(start);
            }
            else {
                Debug.Assert(_lender < 0 && _id < 0);
                Debug.Assert(_array is null && start == 0 && _byteLength == 0);
                _byteLength = sizeof(T) * length;
                _start = Marshal.AllocHGlobal(_byteLength);
            }
        }

        /// <summary>
        /// ※ 絶対に二重解放してはいけない。構造体は二重解放を検知して防止することができない。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Dispose()
        {
            // 安全のために各フィールドは0で初期化するが、構造体は値型なので
            // Dispose 前にインスタンス自体がコピーされている場合、
            // Dispose を呼んでいないインスタンスのフィールドは0初期化されない。
            // そのため複数回 Dispose 呼ぶことが出来てしまう。
            // ファイルの上のコメント参照。

            if(_array is null) {
                // new ValueTypeRentMemory().Dispose() した場合 _array is null だが
                // _start も 0 なので問題ない。Marshal.FreeHGlobal は 0 の時は何もしないことが保証されている。
                Marshal.FreeHGlobal(_start);
            }
            else if(!IsEmpty) {
                MemoryPool.ReturnByteMemory(_lender, _id);
                Unsafe.AsRef(_array) = null!;
            }
            Unsafe.AsRef(_lender) = 0;
            Unsafe.AsRef(_id) = 0;
            Unsafe.AsRef(_start) = IntPtr.Zero;
            Unsafe.AsRef(_byteLength) = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj) => obj is ValueTypeRentMemory<T> memory && Equals(memory);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => HashCode.Combine(_array, _start, _byteLength, _id, _lender);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ValueTypeRentMemory<T> other)
        {
            return ReferenceEquals(_array, other._array) &&
                   _start.Equals(other._start) &&
                   _byteLength == other._byteLength &&
                   _id == other._id &&
                   _lender == other._lender;
        }
    }
}
