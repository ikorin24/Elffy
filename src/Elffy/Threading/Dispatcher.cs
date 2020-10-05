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
        [ThreadStatic]
        private static bool _isMainThread;
        private static bool _isSetMainThread;

        /// <summary>メインスレッドに Invoke された処理を保持しておくためのスレッドセーフなキュー</summary>
        private static readonly ConcurrentQueue<(Action<object?>? action, object? state)> _invokedActions
            = new ConcurrentQueue<(Action<object?>? action, object? state)>();


        // キューの監視はメインスレッド内の同一フレームで複数回行われる可能性がある (windowが複数あるような場合)
        // そのため、メインスレッド内のどのタイミングで実行されるかは保証できない

        /// <summary>
        /// 指定した処理をメインスレッドで行わせます。メインスレッド内のどのタイミングで実行されるかは保証されません。<para/>
        /// 呼び出しスレッドがメインスレッドの場合、処理はキューに送られず、その場で同期的に実行されます。<para/>
        /// それ以外のスレッドからの呼び出しの場合、処理はキューに追加されます。<para/>
        /// </summary>
        /// <param name="action">実行する処理</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Invoke(Action action)
        {
            if(action is null) { ThrowNullArg(); }
            if(IsMainThread()) {
                action!.Invoke();
            }
            else {
                _invokedActions.Enqueue((null, action));
            }

            static void ThrowNullArg() => throw new ArgumentNullException(nameof(action));
        }

        /// <summary>
        /// 指定した処理をメインスレッドで行わせます。メインスレッド内のどのタイミングで実行されるかは保証されません。<para/>
        /// 呼び出しスレッドがメインスレッドの場合、処理はキューに送られず、その場で同期的に実行されます。<para/>
        /// それ以外のスレッドからの呼び出しの場合、処理はキューに追加されます。<para/>
        /// </summary>
        /// <param name="action">実行する処理</param>
        /// <param name="state">実行する処理に渡される引数</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Invoke(Action<object?> action, object? state)
        {
            if(action is null) { ThrowNullArg(); }
            if(IsMainThread()) {
                action!.Invoke(state);
            }
            else {
                _invokedActions.Enqueue((action, state));
            }

            static void ThrowNullArg() => throw new ArgumentNullException(nameof(action));
        }

        /// <summary>現在のスレッドがメインスレッドかどうかを返します。</summary>
        /// <returns>メインスレッドかどうか</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsMainThread()
        {
            Debug.Assert(_isSetMainThread, "Main thread is not set.");
            return _isMainThread;
        }

        /// <summary>現在のスレッドがメインスレッドでない場合、例外を投げます</summary>
        /// <exception cref="InvalidOperationException">現在のスレッドがメインスレッドでないことを示す例外</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNotMainThread()
        {
            if(IsMainThread() == false) { ThrowNotMain(); }

            static void ThrowNotMain() => throw new InvalidOperationException("Current thread is not main thread.");
        }

        /// <summary>
        /// メインスレッドIDを初期化します。最初にメインスレッドから1度だけ呼ばれます。<para/>
        /// <see cref="Current"/> に関わらず、最初に1度だけ呼ばれる。このスレッドが以後 OpenGL を扱う<para/>
        /// </summary>
        internal static void SetMainThreadID()
        {
            Debug.Assert(_isSetMainThread == false);
            _isSetMainThread = true;
            _isMainThread = true;
        }

        /// <summary>Invokedキューの内容を処理します</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void DoInvokedAction()
        {
            // 現時点で既にキュー内にある処理までを実行する。それ以降は次回に処理実行する。
            // このメソッドは必ずメインスレッドから呼ばれ、ここ以外ではキュー内アイテムが減ることはないためスレッドセーフ

            Debug.Assert(IsMainThread());
            var count = _invokedActions.Count;
            if(count > 0) {
                // To minimize fast path, iteration is running in the local function.
                // (Because count of queue items is 0 in the common case.)
                Do(count);
            }

            static void Do(int count)
            {
                while(count > 0 && _invokedActions.TryDequeue(out var item)) {
                    if(item.action is null) {

                        // action が null の時は、引数なしの Action が state としてキューに入っている
                        SafeCast.As<Action>(item.state!).Invoke();
                    }
                    else {
                        item.action(item.state);
                    }
                    count--;
                }
            }
        }
    }
}
