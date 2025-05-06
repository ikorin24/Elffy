#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Effective
{
    public interface IReadOnlySpan<T>
    {
        [UnscopedRef]
        ReadOnlySpan<T> AsReadOnlySpan();
    }
}
