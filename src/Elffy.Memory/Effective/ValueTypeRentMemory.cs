#nullable enable
using Elffy.Effective.Unsafes;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy.Effective
{
    // 構造体をコピーして複数回 Dispose を実行した場合の動作は保証しない。

    // var a = new ValueTypeRentMemory<int>(10);
    // var b = a;
    // a.Dispose();
    // b.Dispose();     // ダメ

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
        // _length : T の要素数
        // _id     : 借りたメモリの識別用番号 (>= 0)
        // _lender : メモリの貸し出し者  (>= 0)
        //
        // [unmanaged メモリを確保した時]
        // _array : null
        // _start : unmanaged メモリのポインタ
        // _length : T の要素数
        // _id     : -1
        // _lender : -1

        private readonly byte[]? _array;
        private readonly IntPtr _start;
        private readonly int _length;
        private readonly int _id;
        private readonly int _lender;

        public unsafe readonly Span<T> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MemoryMarshal.CreateSpan(ref GetReference(), _length);
        }

        public readonly int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _length;
        }

        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _length == 0;
        }

        public readonly ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if((uint)index >= (uint)_length) {
                    ThrowOutOfRange();
                    static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(index));
                }
                return ref Unsafe.Add(ref GetReference(), index);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ValueTypeRentMemory(int length)
        {
            if(length == 0) {
                this = default;
                return;
            }
            if(MemoryPool.TryRentByteMemory<T>(length, out _array, out int start, out _id, out _lender)) {
                Debug.Assert(_array is null == false);
                _start = new IntPtr(start);
            }
            else {
                Debug.Assert(_lender < 0 && _id < 0);
                Debug.Assert(_array is null && start == 0);
                _start = Marshal.AllocHGlobal(sizeof(T) * length);
            }
            _length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref T GetReference()
        {
            if(_array is null) {
                return ref Unsafe.AsRef<T>(_start.ToPointer());
            }
            else {
                return ref Unsafe.As<byte, T>(ref _array.At(_start.ToInt32()));
            }
        }

        /// <summary>複数回このメソッドを呼んだ場合の動作は未定義です</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Dispose()
        {
            if(_array is null) {
                Marshal.FreeHGlobal(_start);
            }
            else if(!IsEmpty) {
                MemoryPool.ReturnByteMemory(_lender, _id);
                Unsafe.AsRef(_array) = null!;
            }
            Unsafe.AsRef(_lender) = 0;
            Unsafe.AsRef(_id) = 0;
            Unsafe.AsRef(_start) = IntPtr.Zero;
            Unsafe.AsRef(_length) = 0;
        }

        public override bool Equals(object? obj) => obj is ValueTypeRentMemory<T> memory && Equals(memory);

        public override int GetHashCode() => HashCode.Combine(_array, _start, _length, _id, _lender);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ValueTypeRentMemory<T> other)
        {
            return ReferenceEquals(_array, other._array) &&
                   _start.Equals(other._start) &&
                   _length == other._length &&
                   _id == other._id &&
                   _lender == other._lender;
        }

        public override string ToString() => DebugDisplay;
    }
}
