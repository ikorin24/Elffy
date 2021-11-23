#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Elffy
{
    internal static class UniTaskSourceHelper
    {
        private static readonly Action<object> _continuationSentinel = ContinuationSentinelAction;
        public static Action<object> ContinuationSentinel => _continuationSentinel;

#if !DEBUG
        [System.Diagnostics.DebuggerHidden]
#endif
        [DoesNotReturn]
        public static void ThrowNullArg(string message) => throw new ArgumentNullException(message);

#if !DEBUG
        [System.Diagnostics.DebuggerHidden]
#endif
        [DoesNotReturn]
        public static void ThrowCannotAwaitTwice() => throw new InvalidOperationException("Cannnot await UniTask twice.");

#if !DEBUG
        [System.Diagnostics.DebuggerHidden]
#endif
        [DoesNotReturn]
        public static void ThrowNotCompleted() => throw new InvalidOperationException("Not yet completed, UniTask only allow to use await.");


#if !DEBUG
        [System.Diagnostics.DebuggerHidden]
#endif
        private static void ContinuationSentinelAction(object _)
        {
            // [NOTE]
            // Don't throw exception here.
            // There is no longer a way to successfully communicate exceptions.
            Debug.Fail("can not invoke continuation twice.");
        }
    }
}
