#nullable enable
using Elffy.Effective.Unsafes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy.Effective
{
    public unsafe readonly struct SpanLike<T> : ISpan<T>, IEquatable<SpanLike<T>>
    {
        private readonly object? _obj;
        private readonly void* _pointer;
        private readonly int _len;

        public static SpanLike<T> Empty => default;

        public bool IsEmpty => _pointer == null && _len == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SpanLike(T[]? array)
        {
            if(array is null) {
                this = default;
            }
            else {
                _obj = array;
                _pointer = (delegate*<object, Span<T>>)&AsSpan;
                _len = 0;
            }

            static Span<T> AsSpan(object obj)
            {
                return MemoryMarshal.CreateSpan(ref Unsafe.As<T[]>(obj).GetReference(), Unsafe.As<T[]>(obj).Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SpanLike(List<T>? list)
        {
            _obj = list;
            _pointer = (delegate*<object, Span<T>>)&AsSpan;
            _len = 0;

            // It's safe if obj is null.
            static Span<T> AsSpan(object obj) => Unsafe.As<List<T>?>(obj).AsSpan();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal SpanLike(object obj, delegate*<object, Span<T>> asSpan)
        {
            Debug.Assert(obj is not null);
            _obj = obj;
            _pointer = asSpan;
            _len = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal SpanLike(void* pointer, int length)
        {
            _obj = null;
            _pointer = pointer;
            _len = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan()
        {
            if(_obj is null) {
                return MemoryMarshal.CreateSpan(ref Unsafe.AsRef<T>(_pointer), _len);
            }
            else {
                // This line may cause compiling error. It's a bug of the compiler. Update your Visual Studio.
                return ((delegate*<object, Span<T>>)_pointer)(_obj);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> AsReadOnlySpan() => AsSpan();

        public ReadOnlySpanLike<T> AsReadOnly() => new ReadOnlySpanLike<T>(_obj, _pointer, _len);

        public override bool Equals(object? obj) => obj is SpanLike<T> like && Equals(like);

        public bool Equals(SpanLike<T> other) => (_obj == other._obj) && (_pointer == other._pointer) && (_len == other._len);

        public override int GetHashCode() => HashCode.Combine(RuntimeHelpers.GetHashCode(_obj!), (IntPtr)_pointer, _len);   // No problem if _obj is null.

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpanLike<T>(SpanLike<T> spanLike) => spanLike.AsReadOnly();
    }

    public static unsafe class SpanLikeExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SpanLike<T> AsSpanLike<T>(this UnsafeRawArray<T> array) where T : unmanaged
        {
            return new SpanLike<T>(array.GetPtr(), array.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpanLike<T> AsReadOnlySpanLike<T>(this UnsafeRawArray<T> array) where T : unmanaged
        {
            return array.AsSpanLike().AsReadOnly();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SpanLike<T> AsSpanLike<T>(this UnsafeRawList<T> list) where T : unmanaged
        {
            return new SpanLike<T>(list.GetPtr(), list.Count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpanLike<T> AsReadOnlySpanLike<T>(this UnsafeRawList<T> list) where T : unmanaged
        {
            return list.AsSpanLike().AsReadOnly();
        }
    }
}
