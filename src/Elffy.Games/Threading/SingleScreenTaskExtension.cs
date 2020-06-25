#nullable enable
using Elffy.Games;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Elffy.Threading
{
    public static class SingleScreenTaskExtension
    {
        /// <summary>指定の <see cref="Task"/> の後に Dispatcher スレッドで処理を行います。</summary>
        /// <param name="task">ソースになる <see cref="Task"/></param>
        /// <param name="action">Dispatcher スレッドで行う処理</param>
        public static void ContinueWithDispatch(this Task task, Action action)
        {
            if(task is null) { throw new ArgumentNullException(nameof(task)); }
            if(action is null) { throw new ArgumentNullException(nameof(action)); }

            // 実質的に
            // await task;
            // action();
            // と同じだが、元のスレッドが同期コンテキスト持ちのスレッドでなくともスレッド復帰でき、async も不要

            task.ContinueWith((t, state) =>
            {
                Debug.Assert(state is Action);
                var action = Unsafe.As<Action>(state);

                if(t.Exception is null) {
                    SingleScreenApp.Dispatcher.Invoke(action);
                }
                else {
                    throw t.Exception;
                }
            }, action);
        }

        /// <summary>指定の <see cref="Task{T}"/> の後に Dispatcher スレッドで処理を行います。</summary>
        /// <typeparam name="T">ソースになるタスクの戻り値の型</typeparam>
        /// <param name="task">ソースになるタスク</param>
        /// <param name="action">Dispatcher スレッドで行う処理</param>
        public static void ContinueWithDispatch<T>(this Task<T> task, Action<T> action)
        {
            if(task is null) { throw new ArgumentNullException(nameof(task)); }
            if(action is null) { throw new ArgumentNullException(nameof(action)); }

            // 実質的に
            // action(await task);
            // と同じだが、元のスレッドが同期コンテキスト持ちのスレッドでなくともスレッド復帰でき、async も不要

            task.ContinueWith((t, state) =>
            {
                Debug.Assert(state is Action<T>);
                var action = Unsafe.As<Action<T>>(state);

                if(t.Exception is null) {
                    SingleScreenApp.Dispatcher.Invoke(() => action(t.Result));  // TODO: ラムダ式のキャプチャ避けるオーバーロードを Invoke に用意する
                }
                else {
                    throw t.Exception;
                }
            }, action);
        }
    }
}
