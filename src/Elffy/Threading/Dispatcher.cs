using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elffy.Threading
{
    public static class Dispatcher
    {
        private static readonly Queue<Action> _invokedActions = new Queue<Action>();
        private static readonly object _syncRoot = new object();
        private static bool _hasMainThreadID;
        private static int _mainThreadID;

        /// <summary>指定した処理をメインスレッドで行わせます。</summary>
        /// <param name="action">実行する処理</param>
        public static void Invoke(Action action)
        {
            if(action == null) { throw new ArgumentNullException(); }
            if(IsMainThread()) {
                action();
            }
            else {
                lock(_syncRoot) {
                    _invokedActions.Enqueue(action);
                }
            }
        }

        /// <summary>現在のスレッドがメインスレッドかどうかを返します。</summary>
        /// <returns>メインスレッドかどうか</returns>
        public static bool IsMainThread()
        {
            ThrowIfNotInitialized();
            return Thread.CurrentThread.ManagedThreadId == _mainThreadID;
        }

        /// <summary>メインスレッドIDを初期化します。最初にメインスレッドから1度だけ呼ばれます。</summary>
        internal static void SetMainThreadID()
        {
            Debug.Assert(_hasMainThreadID == false);
            _mainThreadID = Thread.CurrentThread.ManagedThreadId;
            _hasMainThreadID = true;
        }

        /// <summary>Invokedキューの内容を処理します</summary>
        internal static void DoInvokedAction()
        {
            var count = _invokedActions.Count;
            if(count == 0) { return; }
            lock(_syncRoot) {
                foreach(var action in _invokedActions) {
                    action();
                }
                _invokedActions.Clear();
            }
        }

        private static void ThrowIfNotInitialized()
        {
            if(!_hasMainThreadID) {
                throw new InvalidOperationException("Main Thread ID is not set.");
            }
        }
    }
}
