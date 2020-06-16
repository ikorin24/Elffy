#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elffy.Effective.Internal;

namespace Elffy.Effective
{
    [DebuggerDisplay("{DebugDisplay}")]
    public readonly struct RefTypeRentMemory<T> : IDisposable where T : class
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebugDisplay => $"{nameof(RefTypeRentMemory<T>)}<{typeof(T).Name}>[{Span.Length}]";

        // IMemoryOwner<T> を継承するメリットが特になく、
        // Memory<T> を公開する方法もないので
        // IMemoryOwner<T> は継承しない。

        private readonly Memory<object> _objectMemory;
        private readonly int _id;
        private readonly int _lender;

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
                _objectMemory = new object[length];     // TODO: できれば new は避けたい。あと構造体の等価比較の実装
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Dispose()
        {
            if(!_objectMemory.IsEmpty) {
                _objectMemory.Span.Clear();     // All elements MUST be cleared, or elements are not collected by GC.
                MemoryPool.ReturnObjectMemory(_lender, _id);
            }
        }
    }
}
