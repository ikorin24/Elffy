using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elffy.Threading
{
    public static class GameThread
    {
        private static readonly Queue<Action> _invokedActions = new Queue<Action>();
        private static readonly object _syncRoot = new object();
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
        public static bool IsMainThread() => Thread.CurrentThread.ManagedThreadId == _mainThreadID;

        /// <summary>メインスレッドIDを初期化します。最初にメインスレッドから1度だけ呼ばれます。</summary>
        internal static void SetMainThreadID()
        {
            _mainThreadID = Thread.CurrentThread.ManagedThreadId;
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
    }
}
