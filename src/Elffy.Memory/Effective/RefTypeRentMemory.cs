#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Elffy.Effective.Unsafes;

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
    public readonly struct RefTypeRentMemory<T> : IEquatable<RefTypeRentMemory<T>>, IDisposable where T : class
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebugDisplay => $"{nameof(RefTypeRentMemory<T>)}<{typeof(T).Name}>[{Span.Length}]";

        // IMemoryOwner<T> を継承するメリットが特になく、
        // Memory<T> を公開する方法もないので
        // IMemoryOwner<T> は継承しない。

        private readonly object[]? _array;
        private readonly int _start;
        private readonly int _length;
        private readonly int _id;
        private readonly int _lender;

        public readonly Span<T> Span
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe RefTypeRentMemory(int length)
        {
            if(length == 0) {
                this = default;
                return;
            }
            if(!MemoryPool.TryRentObjectMemory(length, out _array, out _start, out _id, out _lender)) {
                Debug.Assert(_lender < 0 && _id < 0);
                _array = new object[length];
            }
            _length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref T GetReference()
        {
            if(_array is null) {
                return ref Unsafe.AsRef<T>(null);
            }
            else {
                return ref Unsafe.As<object, T>(ref _array.At(_start));
            }
        }

        /// <summary>複数回このメソッドを呼んだ場合の動作は未定義です</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Dispose()
        {
            if(!IsEmpty) {
                Span.Clear();               // All elements MUST be cleared, or elements are not collected by GC.
                MemoryPool.ReturnObjectMemory(_lender, _id);
                Unsafe.AsRef(_array) = null;
                Unsafe.AsRef(_start) = 0;
                Unsafe.AsRef(_length) = 0;
                Unsafe.AsRef(_id) = 0;
                Unsafe.AsRef(_lender) = 0;
            }
        }

        public override bool Equals(object? obj) => obj is RefTypeRentMemory<T> memory && Equals(memory);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(RefTypeRentMemory<T> other)
        {
            return _array == other._array &&
                   _start == other._start &&
                   _length == other._length &&
                   _id == other._id &&
                   _lender == other._lender;
        }

        public override int GetHashCode() => HashCode.Combine(_array, _start, _length, _id, _lender);

        public override string ToString() => DebugDisplay;
    }
}
