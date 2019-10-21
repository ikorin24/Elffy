using System;
using System.Collections.Concurrent;
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
        /// <summary>メインスレッドに Invoke された処理を保持しておくためのスレッドセーフなキュー</summary>
        private static readonly ConcurrentQueue<Action> _invokedActions = new ConcurrentQueue<Action>();
        private static bool _hasMainThreadID;
        private static int _mainThreadID;

        /// <summary>指定した処理をメインスレッドで行わせます。</summary>
        /// <remarks>
        /// 呼び出しスレッドがメインスレッドの場合、処理はキューに送られず、その場で同期的に実行されます。<para/>
        /// それ以外のスレッドからの呼び出しの場合、処理はキューに追加されます。<para/>
        /// </remarks>
        /// <param name="action">実行する処理</param>
        public static void Invoke(Action action)
        {
            if(action == null) { throw new ArgumentNullException(); }
            if(IsMainThread()) {
                action();
            }
            else {
                _invokedActions.Enqueue(action);
            }
        }

        /// <summary>現在のスレッドがメインスレッドかどうかを返します。</summary>
        /// <returns>メインスレッドかどうか</returns>
        public static bool IsMainThread()
        {
            ThrowIfNotInitialized();
            return Thread.CurrentThread.ManagedThreadId == _mainThreadID;
        }

        /// <summary>現在のスレッドがメインスレッドでない場合、例外を投げます</summary>
        /// <exception cref="InvalidOperationException">現在のスレッドがメインスレッドでないことを示す例外</exception>
        public static void ThrowIfNotMainThread()
        {
            if(IsMainThread() == false) { throw new InvalidOperationException("Current thread must be Main Thread."); }
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
            // 現時点で既にキュー内にある処理までを実行する。それ以降は次回に処理実行する。
            var count = _invokedActions.Count;
            while(count > 0 && _invokedActions.TryDequeue(out var action)) {
                action();
                count--;
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
