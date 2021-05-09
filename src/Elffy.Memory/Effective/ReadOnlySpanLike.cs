#nullable enable
using Elffy.Effective.Unsafes;
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
            if(array is null) {
                this = default;
            }
            else {
                _obj = array;
                _pointer = (delegate*<object, ReadOnlySpan<T>>)&AsSpan;
                _len = 0;
            }

            static ReadOnlySpan<T> AsSpan(object obj)
            {
                return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<T[]>(obj).GetReference(), Unsafe.As<T[]>(obj).Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpanLike(List<T>? list)
        {
            _obj = list;
            _pointer = (delegate*<object, ReadOnlySpan<T>>)&AsSpan;
            _len = 0;

            // It's safe if obj is null.
            static ReadOnlySpan<T> AsSpan(object obj) => Unsafe.As<List<T>?>(obj).AsReadOnlySpan();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpanLike(object obj, delegate*<object, Span<T>> asSpan)
        {
            Debug.Assert(obj is not null);
            _obj = obj;
            _pointer = asSpan;
            _len = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ReadOnlySpanLike(void* pointer, int length)
        {
            _obj = null;
            _pointer = pointer;
            _len = length;
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

        public override int GetHashCode() => HashCode.Combine(_obj, (IntPtr)_pointer, _len);
    }
}
