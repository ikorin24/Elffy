﻿using System;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;
using System.Security;
using System.Security.Permissions;

namespace Elffy.Threading
{
    /// <summary>スレッドの同期コンテキストクラスです</summary>
    public sealed class ElffySynchronizationContext : SynchronizationContext
    {
        /// <summary>現在のスレッドの <see cref="SynchronizationContext"/> を生成中かどうか (このメンバはスレッドごとに独立した値を持ちます)</summary>
        [ThreadStatic]
        private static bool _inInstallation;

        /// <summary>今のコンテキストをセットする前のコンテキスト (このメンバはスレッドごとに独立した値を持ちます)</summary>
        [ThreadStatic]
        private static SynchronizationContext _previousSyncContext;

        /// <summary>この <see cref="ElffySynchronizationContext"/> インスタンスが紐づいている <see cref="Thread"/></summary>
        private Thread DestinationThread
        {
            get
            {
                if(_destinationThread?.IsAlive == true) {
                    return _destinationThread.Target as Thread;
                }
                return null;
            }
            set
            {
                if(value != null) {
                    _destinationThread = new WeakReference(value);
                }
            }
        }
        private WeakReference _destinationThread;

        /// <summary>現在のスレッドに紐づけられた同期コンテキストを生成します</summary>
        private ElffySynchronizationContext()
        {
            DestinationThread = Thread.CurrentThread;
        }

        /// <summary>スレッドを指定して同期コンテキストを生成します</summary>
        /// <param name="destinationThread"></param>
        private ElffySynchronizationContext(Thread destinationThread)
        {
            DestinationThread = destinationThread;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            Dispatcher.Invoke(() => d(state));
        }

        /// <summary>現在のインスタンスのコピーを生成します</summary>
        /// <returns>コピーインスタンス</returns>
        public override SynchronizationContext CreateCopy()
        {
            return new ElffySynchronizationContext(DestinationThread);
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
                    AsyncOperationManager.SynchronizationContext = new ElffySynchronizationContext();
                }
            }
            finally {
                _inInstallation = false;
            }
        }

        /// <summary>現在のスレッドの同期コンテキストが <see cref="ElffySynchronizationContext"/> の場合、同期コンテキストを削除します</summary>
        internal static void Delete()
        {
            if(AsyncOperationManager.SynchronizationContext is ElffySynchronizationContext) {
                AsyncOperationManager.SynchronizationContext = _previousSyncContext ?? new SynchronizationContext();
                _previousSyncContext = null;
            }
        }
    }
}