#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Elffy.Effective.Internal;

namespace Elffy.Effective
{
    public readonly struct RefTypeRentMemory<T> : IDisposable where T : class
    {
        // IMemoryOwner<T> を継承するメリットが特になく、
        // Memory<T> を公開する方法もないので
        // IMemoryOwner<T> は継承しない。

        private readonly int _id;
        private readonly int _lender;
        private readonly Memory<object> _objectMemory;

        public readonly Span<T> Span
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => SpanCastUnsafe.CastRefType<object, T>(_objectMemory.Span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe RefTypeRentMemory(int length)
        {
            if(!MemoryPool.TryRentObjectMemory<T>(length, out _objectMemory, out _id, out _lender)) {
                Debug.Assert(_lender < 0);
                _objectMemory = new object[length];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Dispose()
        {
            if(_lender >= 0) {
                _objectMemory.Span.Clear();     // All elements MUST be cleared, or elements are not collected by GC.
                MemoryPool.ReturnObjectMemory(_lender, _id);
            }
        }
    }
}
