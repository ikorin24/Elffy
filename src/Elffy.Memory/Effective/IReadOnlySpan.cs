#nullable enable
using System;

namespace Elffy.Effective
{
    public interface IReadOnlySpan<T>
    {
        ReadOnlySpan<T> AsReadOnlySpan();
    }
}
