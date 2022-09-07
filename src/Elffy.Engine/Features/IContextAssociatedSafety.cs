#nullable enable
using Elffy;
using System;

namespace Elffy.Features;

public interface IContextAssociatedSafety : IDisposable
{
    IHostScreen? AssociatedContext { get; }

    bool IsAssociatedWithCurrentContext()
    {
        var current = Engine.CurrentContext;
        return current != null && current == AssociatedContext;
    }
}
