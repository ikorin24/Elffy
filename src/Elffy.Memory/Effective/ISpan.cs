#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Effective
{
    public interface ISpan<T> : IReadOnlySpan<T>
    {
        [UnscopedRef]
        Span<T> AsSpan();
    }
}
