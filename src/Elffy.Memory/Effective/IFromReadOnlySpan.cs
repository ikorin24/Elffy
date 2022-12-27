#nullable enable
using System;

namespace Elffy
{
    public interface IFromReadOnlySpan<TSelf, TItem>
        where TSelf : IFromReadOnlySpan<TSelf, TItem>
    {
        abstract static TSelf From(ReadOnlySpan<TItem> span);
    }

    public static class FromReadOnlySpanExtensions
    {
        public static TResult Into<T, TResult>(this Span<T> source)
            where TResult : IFromReadOnlySpan<TResult, T>
        {
            return TResult.From((ReadOnlySpan<T>)source);
        }

        public static TResult Into<T, TResult>(this ReadOnlySpan<T> source)
            where TResult : IFromReadOnlySpan<TResult, T>
        {
            return TResult.From(source);
        }
    }
}
