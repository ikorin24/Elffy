#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elffy.Effective.Unsafes;

namespace Elffy.Effective
{
    // ValueTypeRentMemory<T> のコメントも見てください

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

        private readonly Memory<object> _objectMemory;
        private readonly int _id;
        private readonly int _lender;

        public static RefTypeRentMemory<T> Empty => default;

        public readonly Span<T> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => SpanCastUnsafe.CastRefType<object, T>(_objectMemory.Span);
        }

        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _objectMemory.IsEmpty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe RefTypeRentMemory(int length)
        {
            if(length == 0) {
                this = default;
                return;
            }
            if(!MemoryPool.TryRentObjectMemory(length, out _objectMemory, out _id, out _lender)) {
                Debug.Assert(_lender < 0 && _id < 0);
                _objectMemory = new object[length];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Dispose()
        {
            if(!_objectMemory.IsEmpty) {
                _objectMemory.Span.Clear();     // All elements MUST be cleared, or elements are not collected by GC.
                MemoryPool.ReturnObjectMemory(_lender, _id);
                Unsafe.AsRef(_objectMemory) = Memory<object>.Empty;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj) => obj is RefTypeRentMemory<T> memory && Equals(memory);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(RefTypeRentMemory<T> other)
        {
            return _objectMemory.Equals(other._objectMemory) &&
                   _id == other._id &&
                   _lender == other._lender;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => HashCode.Combine(_objectMemory, _id, _lender);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() => DebugDisplay;
    }
}
