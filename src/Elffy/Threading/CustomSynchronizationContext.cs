#nullable enable
using System;
using System.Threading;
using System.ComponentModel;

namespace Elffy.Threading
{
    /// <summary>スレッドの同期コンテキストクラスです</summary>
    public sealed class CustomSynchronizationContext : SynchronizationContext
    {
        private readonly SyncContextReceiver _syncContextReceiver;

        [ThreadStatic]
        private static SynchronizationContext? _previousSyncContext;

        /// <summary>この <see cref="CustomSynchronizationContext"/> インスタンスが紐づいている <see cref="Thread"/></summary>
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

        /// <summary>現在のインスタンスのコピーを生成します</summary>
        /// <returns>コピーインスタンス</returns>
        public override SynchronizationContext CreateCopy()
        {
            return new CustomSynchronizationContext(DestinationThread, _syncContextReceiver);
        }

        internal static void Create(SyncContextReceiver reciever)
        {
            var context = AsyncOperationManager.SynchronizationContext;
            if(context is null || context.GetType() == typeof(SynchronizationContext)) {
                _previousSyncContext = context;
                var newContext = new CustomSynchronizationContext(reciever);
                AsyncOperationManager.SynchronizationContext = newContext;
                AsyncHelper.SetEngineSyncContext(newContext);
            }
        }

        internal static void Delete()
        {
            if(AsyncOperationManager.SynchronizationContext is CustomSynchronizationContext) {
                AsyncOperationManager.SynchronizationContext = _previousSyncContext ?? new SynchronizationContext();
                AsyncHelper.ClearEngineSyncContext();
                _previousSyncContext = null;
            }
        }
    }
}
