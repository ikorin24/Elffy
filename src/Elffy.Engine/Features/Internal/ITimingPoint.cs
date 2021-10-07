#nullable enable
using System;

namespace Elffy.Features.Internal
{
    internal interface ITimingPoint
    {
        void Post(Action continuation);
        void Post(Action<object?> continuation, object? state);
    }
}
