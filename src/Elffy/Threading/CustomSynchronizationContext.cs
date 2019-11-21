#nullable enable
using System;
using System.Threading;
using System.ComponentModel;

namespace Elffy.Threading
{
    /// <summary>スレッドの同期コンテキストクラスです</summary>
    public sealed class CustomSynchronizationContext : SynchronizationContext
    {
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

        /// <summary>現在のスレッドに紐づけられた同期コンテキストを生成します</summary>
        private CustomSynchronizationContext()
        {
            _destinationThread = new WeakReference(Thread.CurrentThread);
        }

        /// <summary>スレッドを指定して同期コンテキストを生成します</summary>
        /// <param name="destinationThread"></param>
        private CustomSynchronizationContext(Thread? destinationThread)
        {
            _destinationThread = new WeakReference(destinationThread);
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            Dispatcher.Invoke(() => d(state));
        }

        /// <summary>現在のインスタンスのコピーを生成します</summary>
        /// <returns>コピーインスタンス</returns>
        public override SynchronizationContext CreateCopy()
        {
            return new CustomSynchronizationContext(DestinationThread);
        }

        /// <summary>現在のスレッドに同期コンテキストを生成する必要がある場合、生成します</summary>
        internal static void CreateIfNeeded()
        {
            if(_inInstallation) { return; }

            if(Current == null) { _previousSyncContext = null; }

            if(_previousSyncContext != null) { return; }

            _inInstallation = true;
            try {
                var context = AsyncOperationManager.SynchronizationContext;
                if(context == null || context.GetType() == typeof(SynchronizationContext)) {        // コンテキストが null またはデフォルトコンテキスト
                    _previousSyncContext = context;
                    AsyncOperationManager.SynchronizationContext = new CustomSynchronizationContext();
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
