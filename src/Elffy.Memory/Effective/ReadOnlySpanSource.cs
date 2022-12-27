#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy.Effective
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe readonly struct ReadOnlySpanSource<T> : IReadOnlySpan<T>, IEquatable<ReadOnlySpanSource<T>>
    {
        // Fields must be same layout as SpanSource<T>

        private readonly object? _obj;
        private readonly delegate*<in SpanSource<T>, Span<T>> _asSpan;
        private readonly int _start;
        private readonly int _len;

        public static ReadOnlySpanSource<T> Empty => default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpanSource(T[]? array)
        {
            this = new SpanSource<T>(array).AsReadOnly();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpanSource(T[] array, int start)
        {
            this = new SpanSource<T>(array, start).AsReadOnly();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpanSource(T[] array, int start, int length)
        {
            this = new SpanSource<T>(array, start, length).AsReadOnly();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpanSource(List<T>? list)
        {
            this = new SpanSource<T>(list).AsReadOnly();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> AsReadOnlySpan()
        {
            return Unsafe.As<ReadOnlySpanSource<T>, SpanSource<T>>(ref Unsafe.AsRef<ReadOnlySpanSource<T>>(in this)).AsSpan();
        }

        public override bool Equals(object? obj) => obj is ReadOnlySpanSource<T> s && Equals(s);

        public bool Equals(ReadOnlySpanSource<T> other)
        {
            // There is no need to check `_asSpan`.
            return ReferenceEquals(_obj, other._obj) && (_start == other._start) && (_len == other._len);
        }

        public override int GetHashCode()
        {
            // There is no need to check `_asSpan`.
            return HashCode.Combine(RuntimeHelpers.GetHashCode(_obj), _start, _len);
        }
    }
}
