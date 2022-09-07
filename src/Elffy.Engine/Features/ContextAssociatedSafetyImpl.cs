#nullable enable
using System;

namespace Elffy.Features;

public struct ContextAssociatedSafetyImpl
{
    private bool _isSafetyRegistered;
    private IHostScreen? _associatedContext;

    public readonly IHostScreen? AssociatedContext => _associatedContext;

    public readonly bool IsAssociatedWithCurrentContext()
    {
        var current = Engine.CurrentContext;
        return current != null && current == _associatedContext;
    }

    public bool TryRegister<T>(T obj, IHostScreen screen, Action<T> release) where T : class, IContextAssociatedSafety
    {
        if(obj == null) { return false; }
        if(screen == null) { return false; }
        if(_isSafetyRegistered) {
            return false;
        }
        ContextAssociatedMemorySafety.Register(obj, screen);
        _associatedContext = screen;
        _isSafetyRegistered = true;
        return true;
    }

    public bool TryRegisterToCurrentContext<T>(T obj, Action<T> disposeAction) where T : class, IContextAssociatedSafety
    {
        if(obj == null) { return false; }
        var screen = Engine.CurrentContext;
        if(screen == null) { return false; }
        return TryRegister(obj, screen, disposeAction);
    }

    public readonly void OnFinalized<T>(T obj) where T : class, IContextAssociatedSafety
    {
        if(_isSafetyRegistered) {
            ContextAssociatedMemorySafety.OnFinalized(obj);
        }
    }

    public readonly void ThrowIfAssociatedContextMismatch()
    {
        var associatedContext = AssociatedContext;
        if(associatedContext == null) {
            return;
        }
        var currentContext = Engine.CurrentContext;
        if(currentContext == null) {
            ContextMismatchException.Throw(currentContext, associatedContext);
        }
        ContextMismatchException.ThrowIfContextNotEqual(currentContext, associatedContext);
    }
}
