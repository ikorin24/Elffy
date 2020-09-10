#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace Elffy.Threading
{
    public static class AsyncHelper
    {
        private static CustomSynchronizationContext? _syncContext;

        public static SwitchToSynchronizationContextAwaitable SwitchToMain(CancellationToken cancellationToken = default)
        {
            if(_syncContext is null) { ThrowNullSyncContext(); }

            return UniTask.SwitchToSynchronizationContext(_syncContext, cancellationToken);
        }

        internal static void SetEngineSyncContext(CustomSynchronizationContext context)
        {
            Debug.Assert(_syncContext is null);
            _syncContext = context;
        }

        internal static void ClearEngineSyncContext()
        {
            _syncContext = null;
        }

        [DoesNotReturn]
        static void ThrowNullSyncContext() => throw new InvalidOperationException("Engine is not running.");
    }
}

