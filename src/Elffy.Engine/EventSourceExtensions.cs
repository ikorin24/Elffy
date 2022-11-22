#nullable enable
using System.Threading;

namespace Elffy;

internal static class EventSourceExtensions
{
    public static void InvokeIgnoreException<T>(this EventSource<T> eventSource, T arg)
    {
        try {
            eventSource.Invoke(arg);
        }
        catch {
            if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
            // Don't throw. (Ignore exceptions in user code)
        }
    }

    public static void InvokeIgnoreException<T>(this AsyncEventSource<T> eventSource, T arg, CancellationToken cancellationToken)
    {
        try {
            eventSource.Invoke(arg, cancellationToken);
        }
        catch {
            if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
            // Don't throw. (Ignore exceptions in user code)
        }
    }

    public static void InvokeSequentiallyIgnoreException<T>(this AsyncEventSource<T> eventSource, T arg, CancellationToken cancellationToken)
    {
        try {
            eventSource.InvokeSequentially(arg, cancellationToken);
        }
        catch {
            if(EngineSetting.UserCodeExceptionCatchMode == UserCodeExceptionCatchMode.Throw) { throw; }
            // Don't throw. (Ignore exceptions in user code)
        }
    }
}
