#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy.Effective
{
    public unsafe readonly struct ReadOnlySpanLike<T> : IReadOnlySpan<T>, IEquatable<ReadOnlySpanLike<T>>
    {
        private readonly object? _obj;
        private readonly void* _pointer;
        private readonly int _len;

        public static ReadOnlySpanLike<T> Empty => default;

        public bool IsEmpty => _pointer == null && _len == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpanLike(T[]? array)
        {
            this = new SpanLike<T>(array).AsReadOnly();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpanLike(List<T>? list)
        {
            this = new SpanLike<T>(list).AsReadOnly();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpanLike(object? obj, void* pointer, int length)
        {
            // This constructor is called from cast from SpanLike<T>

            _obj = obj;
            _pointer = pointer;
            _len = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> AsReadOnlySpan()
        {
            if(_obj is null) {
                return MemoryMarshal.CreateSpan(ref Unsafe.AsRef<T>(_pointer), _len);
            }
            else {
                // This line may cause compiling error. It's a bug of the compiler. Update your Visual Studio.
                return ((delegate*<object, ReadOnlySpan<T>>)_pointer)(_obj);
            }
        }

        public override bool Equals(object? obj) => obj is ReadOnlySpanLike<T> like && Equals(like);

        public bool Equals(ReadOnlySpanLike<T> other) => (_obj == other._obj) && (_pointer == other._pointer) && (_len == other._len);

        public override int GetHashCode() => HashCode.Combine(RuntimeHelpers.GetHashCode(_obj!), (IntPtr)_pointer, _len);   // No problem if _obj is null.
    }
}
