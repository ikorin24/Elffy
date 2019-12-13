#nullable enable
using System;
using System.Threading;
using System.ComponentModel;
using Elffy.Core;

namespace Elffy.Threading
{
    /// <summary>スレッドの同期コンテキストクラスです</summary>
    public sealed class CustomSynchronizationContext : SynchronizationContext
    {
        private readonly SyncContextReceiver _syncContextReceiver;
        /// <summary>現在のスレッドの <see cref="SynchronizationContext"/> を生成中かどうか (このメンバはスレッドごとに独立した値を持ちます)</summary>
        [ThreadStatic]
        private static bool _inInstallation;

        /// <summary>今のコンテキストをセットする前のコンテキスト (このメンバはスレッドごとに独立した値を持ちます)</summary>
        [ThreadStatic]
        private static SynchronizationContext? _previousSyncContext;

        /// <summary>この <see cref="CustomSynchronizationContext"/> インスタンスが紐づいている <see cref="Thread"/></summary>
        private Thread? DestinationThread
        {
            get
            {
                if(_destinationThread?.IsAlive == true) {
                    return _destinationThread.Target as Thread;
                }
                return null;
            }
        }
        private readonly WeakReference _destinationThread;

        /////// <summary>現在のスレッドに紐づけられた同期コンテキストを生成します</summary>
        
        /// <summary>Create synchronization context of current thread.</summary>
        /// <param name="receiver">Synchronization context receiver</param>
        private CustomSynchronizationContext(SyncContextReceiver receiver)
        {
            _destinationThread = new WeakReference(Thread.CurrentThread);
            _syncContextReceiver = receiver;
        }

        ///// <summary>スレッドを指定して同期コンテキストを生成します</summary>
        ///// <param name="destinationThread"></param>

        /// <summary>Create synchronization context of specified thread.</summary>
        /// <param name="destinationThread">Target thread of context</param>
        /// <param name="receiver">Synchronization context receiver</param>
        private CustomSynchronizationContext(Thread? destinationThread, SyncContextReceiver receiver)
        {
            _destinationThread = new WeakReference(destinationThread);
            _syncContextReceiver = receiver;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            _syncContextReceiver.Add(() => d(state));
        }

        /// <summary>現在のインスタンスのコピーを生成します</summary>
        /// <returns>コピーインスタンス</returns>
        public override SynchronizationContext CreateCopy()
        {
            return new CustomSynchronizationContext(DestinationThread, _syncContextReceiver);
        }

        /// <summary>現在のスレッドに同期コンテキストを生成する必要がある場合、生成します</summary>
        internal static void CreateIfNeeded(SyncContextReceiver reciever)
        {
            if(_inInstallation) { return; }

            if(Current == null) { _previousSyncContext = null; }

            if(_previousSyncContext != null) { return; }

            _inInstallation = true;
            try {
                var context = AsyncOperationManager.SynchronizationContext;
                if(context == null || context.GetType() == typeof(SynchronizationContext)) {        // コンテキストが null またはデフォルトコンテキスト
                    _previousSyncContext = context;
                    AsyncOperationManager.SynchronizationContext = new CustomSynchronizationContext(reciever);
                }
            }
            finally {
                _inInstallation = false;
            }
        }

        /// <summary>現在のスレッドの同期コンテキストが <see cref="CustomSynchronizationContext"/> の場合、同期コンテキストを削除します</summary>
        internal static void Delete()
        {
            if(AsyncOperationManager.SynchronizationContext is CustomSynchronizationContext) {
                AsyncOperationManager.SynchronizationContext = _previousSyncContext ?? new SynchronizationContext();
                _previousSyncContext = null;
            }
        }
    }
}
