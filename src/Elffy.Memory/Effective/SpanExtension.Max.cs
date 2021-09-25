#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Effective
{
    public static partial class SpanExtension
    {
        public static int Max(this Span<int> source) => Max(source.AsReadOnly());

        public static int Max(this ReadOnlySpan<int> source)
        {
            if(source.IsEmpty) { ThrowEmptySpan(); }
            var max = source[0];
            foreach(var value in source.Slice(1)) {
                max = Math.Max(value, max);
            }
            return max;
        }

        public static uint Max(this Span<uint> source) => Max(source.AsReadOnly());

        public static uint Max(this ReadOnlySpan<uint> source)
        {
            if(source.IsEmpty) { ThrowEmptySpan(); }
            var max = source[0];
            foreach(var value in source.Slice(1)) {
                max = Math.Max(value, max);
            }
            return max;
        }

        public static long Max(this Span<long> source) => Max(source.AsReadOnly());

        public static long Max(this ReadOnlySpan<long> source)
        {
            if(source.IsEmpty) { ThrowEmptySpan(); }
            var max = source[0];
            foreach(var value in source.Slice(1)) {
                max = Math.Max(value, max);
            }
            return max;
        }

        public static ulong Max(this Span<ulong> source) => Max(source.AsReadOnly());

        public static ulong Max(this ReadOnlySpan<ulong> source)
        {
            if(source.IsEmpty) { ThrowEmptySpan(); }
            var max = source[0];
            foreach(var value in source.Slice(1)) {
                max = Math.Max(value, max);
            }
            return max;
        }

        [DoesNotReturn]
        private static void ThrowEmptySpan() => throw new ArgumentException("The span is empty.");
    }
}
