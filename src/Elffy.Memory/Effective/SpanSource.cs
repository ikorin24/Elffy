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
    [StructLayout(LayoutKind.Sequential)]
    public unsafe readonly struct SpanSource<T> : ISpan<T>, IEquatable<SpanSource<T>>
    {
        // Fields must be same layout as ReadOnlySpanSource<T>

        private readonly object? _obj;
        private readonly delegate*<in SpanSource<T>, Span<T>> _asSpan;
        private readonly int _start;
        private readonly int _len;

        public static SpanSource<T> Empty => default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SpanSource(T[]? array)
        {
            if(array is null) {
                this = default;
            }
            else {
                _obj = array;
                _asSpan = &CreateSpan;
                _start = 0;
                _len = array.Length;
            }

            static Span<T> CreateSpan(in SpanSource<T> self)
            {
                var array = Unsafe.As<T[]>(self._obj);
                Debug.Assert(array is not null);
                return MemoryMarshal.CreateSpan(ref array.GetReference(), self._len);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SpanSource(T[] array, int start)
        {
            ArgumentNullException.ThrowIfNull(array);
            if((uint)start >= (uint)array.Length) { throw new ArgumentOutOfRangeException(nameof(start)); }

            _obj = array;
            _asSpan = &CreateSpan;
            _start = start;
            _len = array.Length - start;

            static Span<T> CreateSpan(in SpanSource<T> self)
            {
                var array = Unsafe.As<T[]>(self._obj);
                Debug.Assert(array is not null);
                return MemoryMarshal.CreateSpan(ref Unsafe.Add(ref array.GetReference(), self._start), self._len);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SpanSource(T[] array, int start, int length)
        {
            ArgumentNullException.ThrowIfNull(array);
            if((uint)start >= (uint)array.Length) { throw new ArgumentOutOfRangeException(nameof(start)); }
            if((uint)length > (uint)(array.Length - start)) { throw new ArgumentOutOfRangeException(nameof(length)); }

            _obj = array;
            _asSpan = &CreateSpan;
            _start = start;
            _len = length;

            static Span<T> CreateSpan(in SpanSource<T> self)
            {
                var array = Unsafe.As<T[]>(self._obj);
                Debug.Assert(array is not null);
                return MemoryMarshal.CreateSpan(ref Unsafe.Add(ref array.GetReference(), self._start), self._len);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SpanSource(List<T>? list)
        {
            _obj = list;
            _asSpan = &CreateSpan;
            _start = 0;
            _len = 0;

            // It's safe if obj is null.
            static Span<T> CreateSpan(in SpanSource<T> self) => Unsafe.As<List<T>?>(self._obj).AsSpan();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnscopedRef]
        public Span<T> AsSpan()
        {
            if(_obj is null) {
                return MemoryMarshal.CreateSpan(ref Unsafe.Add(ref Unsafe.AsRef<T>(_asSpan), _start), _len);
            }
            else {
                return _asSpan(in this);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnscopedRef]
        public ReadOnlySpan<T> AsReadOnlySpan() => AsSpan();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpanSource<T> AsReadOnly() => Unsafe.As<SpanSource<T>, ReadOnlySpanSource<T>>(ref Unsafe.AsRef<SpanSource<T>>(in this));

        public override bool Equals(object? obj) => obj is SpanSource<T> s && Equals(s);

        public bool Equals(SpanSource<T> other)
        {
            // There is no need to check `_asSpan`.
            return ReferenceEquals(_obj, other._obj) && (_start == other._start) && (_len == other._len);
        }

        public override int GetHashCode()
        {
            // There is no need to check `_asSpan`.
            return HashCode.Combine(RuntimeHelpers.GetHashCode(_obj), _start, _len);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpanSource<T>(SpanSource<T> spanSource) => spanSource.AsReadOnly();
    }

    public static class SpanSourceExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SpanSource<T> AsSpanSource<T>(this T[]? array) => new SpanSource<T>(array);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SpanSource<T> AsSpanSource<T>(this T[] array, int start) => new SpanSource<T>(array, start);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SpanSource<T> AsSpanSource<T>(this T[] array, int start, int length) => new SpanSource<T>(array, start, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpanSource<T> AsReadOnlySpanSource<T>(this T[]? array) => new ReadOnlySpanSource<T>(array);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpanSource<T> AsReadOnlySpanSource<T>(this T[]? array, int start) => new ReadOnlySpanSource<T>(array, start);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpanSource<T> AsReadOnlySpanSource<T>(this T[]? array, int start, int length) => new ReadOnlySpanSource<T>(array, start, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SpanSource<T> AsSpanSource<T>(this List<T>? list) => new SpanSource<T>(list);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpanSource<T> AsReadOnlySpanSource<T>(this List<T>? list) => new ReadOnlySpanSource<T>(list);
    }
}
