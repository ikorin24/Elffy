#nullable enable
using Elffy.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Elffy.Threading
{
    public static class Dispatcher
    {
        /// <summary>メインスレッドに Invoke された処理を保持しておくためのスレッドセーフなキュー</summary>
        private static readonly ConcurrentQueue<(Action<object?>? action, object? state)> _invokedActions
            = new ConcurrentQueue<(Action<object?>? action, object? state)>();
        private static bool _hasMainThreadID;
        private static int _mainThreadID;

        /// <summary>
        /// 指定した処理をメインスレッドで行わせます。<para/>
        /// 呼び出しスレッドがメインスレッドの場合、処理はキューに送られず、その場で同期的に実行されます。<para/>
        /// それ以外のスレッドからの呼び出しの場合、処理はキューに追加され、現在のフレームの描画処理前に実行されます。<para/>
        /// </summary>
        /// <param name="action">実行する処理</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Invoke(Action action)
        {
            if(action is null) { throw new ArgumentNullException(nameof(action)); }
            if(IsMainThread()) {
                action();
            }
            else {
                _invokedActions.Enqueue((null, action));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Invoke(Action<object?> action, object? state)
        {
            if(action is null) { throw new ArgumentNullException(nameof(action)); }
            if(IsMainThread()) {
                action(state);
            }
            else {
                _invokedActions.Enqueue((action, state));
            }
        }

        /// <summary>現在のスレッドがメインスレッドかどうかを返します。</summary>
        /// <returns>メインスレッドかどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMainThread()
        {
            if(!_hasMainThreadID) { throw new InvalidOperationException("Main Thread ID is not set."); }
            return Thread.CurrentThread.ManagedThreadId == _mainThreadID;
        }

        /// <summary>現在のスレッドがメインスレッドでない場合、例外を投げます</summary>
        /// <exception cref="InvalidOperationException">現在のスレッドがメインスレッドでないことを示す例外</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNotMainThread()
        {
            if(IsMainThread() == false) { throw new InvalidOperationException("Current thread must be Main Thread."); }
        }

        /// <summary>
        /// メインスレッドIDを初期化します。最初にメインスレッドから1度だけ呼ばれます。<para/>
        /// <see cref="Current"/> に関わらず、最初に1度だけ呼ばれる。このスレッドが以後 OpenGL を扱う<para/>
        /// </summary>
        internal static void SetMainThreadID()
        {
            Debug.Assert(_hasMainThreadID == false);
            _mainThreadID = Thread.CurrentThread.ManagedThreadId;
            _hasMainThreadID = true;
        }

        /// <summary>Invokedキューの内容を処理します</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DoInvokedAction()
        {
            // 現時点で既にキュー内にある処理までを実行する。それ以降は次回に処理実行する。
            // このメソッドは必ずメインスレッドから呼ばれ、ここ以外ではキュー内アイテムが減ることはないためスレッドセーフ
            var count = _invokedActions.Count;
            while(count > 0 && _invokedActions.TryDequeue(out var item)) {
                if(item.action is null) {

                    // action が null の時は、引数なしの Action が state としてキューに入っている

                    Debug.Assert(item.state is Action);
                    Unsafe.As<Action>(item.state).Invoke();
                }
                else {
                    item.action(item.state);
                }
                count--;
            }
        }
    }
}
