#nullable enable
using System;

namespace Elffy.Effective
{
    public interface ISpan<T> : IReadOnlySpan<T>
    {
        Span<T> AsSpan();
    }
}
