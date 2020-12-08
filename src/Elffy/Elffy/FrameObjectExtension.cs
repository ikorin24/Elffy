#nullable enable
using System;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;
using Elffy.Core;

namespace Elffy
{
    public static class FrameObjectExtension
    {
        // =======================================================================================
        // [State Transition]
        //
        // [@] : call Activate()
        // [#] : call Terminate()
        // [!] : load completed
        // ---------------------------------------------------------------------------------------------
        //      Frame Num:     |   0   | .. |         n        |  n+1  | .. |         m        |   m+1  | ..
        //     User Action:    |       | .. |  [@]             |       | .. |  [#]             |        | ..
        //        State:       |      New       |   Activated  |      Alive     |  Terminated  |   Dead   ..
        //       IsLoaded:     |               false               [!]            true         |   false  ..
        // Rendered on Screen: |                 No                 | ********** Yes ********* |    No    ..
        // ---------------------------------------------------------------------------------------------
        //
        // Only one state flag is true from 'New' to 'Dead'
        // State get Alive in the next frame of Activate() called.
        // State get Dead in the next frame of Terminated() called.
        // 
        // 'IsLoaded' is independent from them.
        // 'IsLoaded' get true during 'Alive'. (or it may remain false if Terminated() called before load completion.)
        // 
        // Renderd On Screen : IsLoaded && (IsAlive || IsTerminated)
        //
        // =======================================================================================

        /// <summary>Wait until <see cref="Renderable.IsLoaded"/> get true. (If not <see cref="Renderable"/>, no waiting.)</summary>
        /// <param name="timing">frame loop timing to get back</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        public static UniTask WaitUntilLoaded(this FrameObject source, FrameLoopTiming timing = FrameLoopTiming.Update, CancellationToken cancellationToken = default)
        {
            if(source is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(source));
            }

            // [NoTE] See class comment of state transition

            // Early return if not Renderable or already loaded.
            if(source is Renderable renderable) {
                return renderable.IsLoaded ? UniTask.CompletedTask : Wait(renderable, timing, cancellationToken);
            }
            return UniTask.CompletedTask;

            static async UniTask Wait(Renderable renderable, FrameLoopTiming timing, CancellationToken cancellationToken)
            {
                while(renderable.IsLoaded == false) {
                    await renderable.HostScreen.AsyncBack.ToFrameLoopEvent(timing, cancellationToken);
                }
            }
        }
    }
}
