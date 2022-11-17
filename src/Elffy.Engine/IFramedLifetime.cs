#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace Elffy
{
    public interface IFramedLifetime<T>
    {
        IHostScreen? Screen { get; }
        LifeState LifeState { get; }
        AsyncEvent<T> Activating { get; }
        AsyncEvent<T> Terminating { get; }
        Event<T> Alive { get; }
        Event<T> Dead { get; }
        SubscriptionRegister Subscriptions { get; }
        bool TryGetScreen([MaybeNullWhen(false)] out IHostScreen screen);
        IHostScreen GetValidScreen();
    }
}
