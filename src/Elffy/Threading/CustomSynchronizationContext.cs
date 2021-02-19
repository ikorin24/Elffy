#nullable enable
using System;
using System.Threading;
using System.ComponentModel;

namespace Elffy.Threading
{
    /// <summary>Custom <see cref="SynchronizationContext"/> context class</summary>
    public sealed class CustomSynchronizationContext : SynchronizationContext
    {
        private readonly SyncContextReceiver _syncContextReceiver;

        [ThreadStatic]
        private static SynchronizationContext? _previousSyncContext;

        /// <summary>Get <see cref="Thread"/> related with this <see cref="CustomSynchronizationContext"/>.</summary>
        private Thread? DestinationThread 
            => _destinationThread.TryGetTarget(out var thread) ? thread : null;
        private readonly WeakReference<Thread> _destinationThread;
        
        /// <summary>Create synchronization context of current thread.</summary>
        /// <param name="receiver">Synchronization context receiver</param>
        private CustomSynchronizationContext(SyncContextReceiver receiver)
        {
            _destinationThread = new WeakReference<Thread>(Thread.CurrentThread);
            _syncContextReceiver = receiver;
        }

        /// <summary>Create synchronization context of specified thread.</summary>
        /// <param name="destinationThread">Target thread of context</param>
        /// <param name="receiver">Synchronization context receiver</param>
        private CustomSynchronizationContext(Thread? destinationThread, SyncContextReceiver receiver)
        {
            _destinationThread = new WeakReference<Thread>(destinationThread!);     // It is legal that `destinationThread` is null.
            _syncContextReceiver = receiver;
        }

        public override void Post(SendOrPostCallback d, object? state)
        {
            _syncContextReceiver.Add(d, state);
        }

        /// <summary>Create a copy of <see cref="SynchronizationContext"/></summary>
        /// <returns>copied instance of <see cref="SynchronizationContext"/></returns>
        public override SynchronizationContext CreateCopy()
        {
            return new CustomSynchronizationContext(DestinationThread, _syncContextReceiver);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static bool CreateIfNeeded(out CustomSynchronizationContext? syncContext, out SyncContextReceiver? reciever)
        {
            var context = AsyncOperationManager.SynchronizationContext;
            if(context is null || context.GetType() == typeof(SynchronizationContext)) {
                _previousSyncContext = context;
                reciever = new SyncContextReceiver();
                syncContext = new CustomSynchronizationContext(reciever);
                AsyncOperationManager.SynchronizationContext = syncContext;
                return true;
            }
            else {
                syncContext = null;
                reciever = null;
                return false;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Restore()
        {
            if(AsyncOperationManager.SynchronizationContext is CustomSynchronizationContext) {
                AsyncOperationManager.SynchronizationContext = _previousSyncContext!;   // It is valid to set null
                _previousSyncContext = null;
            }
        }
    }
}
