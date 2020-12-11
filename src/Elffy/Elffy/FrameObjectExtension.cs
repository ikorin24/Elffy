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

        /// <summary>Wait until <see cref="Renderable.IsLoaded"/> get true or <see cref="FrameObject.LifeState"/> get <see cref="LifeState.Dead"/>. </summary>
        /// <remarks>If not <see cref="Renderable"/>, no waiting.</remarks>
        /// <param name="timing">frame loop timing to get back</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        public static UniTask<bool> WaitLoadedOrDead(this FrameObject source, FrameLoopTiming timing = FrameLoopTiming.Update, CancellationToken cancellationToken = default)
        {
            if(source is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(source));
            }

            // [NOTE] See class comment of state transition

            // Early return if not Renderable or already loaded.
            if(source is Renderable renderable) {
                if(renderable.HostScreen.IsThreadMain()) {
                    if(renderable.IsLoaded && renderable.LifeState != LifeState.Dead) {
                        return new(true);
                    }
                    else {
                        return Wait(true, renderable, timing, cancellationToken);
                    }
                }
                else {
                    return Wait(false, renderable, timing, cancellationToken);
                }
            }
            else {
                return (source.LifeState == LifeState.New || source.LifeState == LifeState.Dead) ? new(false) : new(true);
            }

            static async UniTask<bool> Wait(bool isMainThreadNow, Renderable renderable, FrameLoopTiming timing, CancellationToken cancellationToken)
            {
                if(!isMainThreadNow) {
                    await renderable.HostScreen.AsyncBack.ToFrameLoopEvent(timing, cancellationToken);
                }
                while(true) {
                    if(renderable.LifeState == LifeState.Dead) {
                        return false;
                    }
                    else if(renderable.IsLoaded) {
                        return true;
                    }
                    else {
                        await renderable.HostScreen.AsyncBack.ToFrameLoopEvent(timing, cancellationToken);
                    }
                }
            }
        }
    }
}
