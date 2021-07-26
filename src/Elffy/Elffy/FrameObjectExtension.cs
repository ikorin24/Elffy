#nullable enable
using System;
using System.Threading;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Cysharp.Threading.Tasks;
using Elffy.Core;

namespace Elffy
{
    internal static class FrameObjectExtension
    {
        // =======================================================================================
        // [State Transition]
        //
        // [@] : call Activate()
        // [#] : call Terminate()
        // [!] : load completed
        // ------------------------------------------------------------------------------------------------------------------------------
        //         Frame Num:         |       0       | .. |        n        |        n+1       | .. |        m        |       m+1      | ..
        //                            |               | .. |                 |                  | .. |                 |                | ..
        //        User Action:        |               | .. |  [@]            |                  | .. |  [#]            |                | ..
        //           State:           |           New          |  Activated  |           Alive           | Terminated  |      Dead      | ..
        //         IsRunning:         |                 false                | ***************** true **************** |      false     | ..
        //      IsLoaded (sync):      |         false         [!] ********************** true ************************ |      false     | ..
        //   IsLoaded (async, case1): |                 false                    [!] *********** true **************** |      false     | ..
        //   IsLoaded (async, case2): |                                    false                                       |      false     | ..
        //                            |               | .. |                 |                  | .. |                 |                | ..
        //                            |               | .. |                 |                  | .. |                 |                | ..
        // ------------------------------------------------------------------------------------------------------------------------------
        //
        // Only one state flag is true from 'New' to 'Dead'
        // State get Alive in the next frame of Activate() called.
        // State get Dead in the next frame of Terminated() called.
        // 
        // In the case of sync loading, 'IsLoaded' get true when Activate() is called.
        // In the case of async loading, 'IsLoaded' get true between Activate() and Terminated(). (case1)
        // If async loading is not completed before Terminated(), 'IsLoaded' remains false. (case2)
        // 
        // Renderd on the Screen : IsLoaded && (IsAlive || IsTerminated)
        //
        // =======================================================================================

        /// <summary>Wait until <see cref="Renderable.IsLoaded"/> get true or <see cref="FrameObject.LifeState"/> get <see cref="LifeState.Dead"/>. </summary>
        /// <remarks>
        /// Returns false if <paramref name="source"/> is <see cref="LifeState.Dead"/>. In that case, <paramref name="timing"/> is not guaranteed.<para/>
        /// Throws <see cref="ArgumentException"/> if <paramref name="source"/> is <see cref="LifeState.New"/>.
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="source"/> is not activated.</exception>
        /// <exception cref="ArgumentNullException"></exception>
        /// <param name="source">source instance</param>
        /// <param name="timing">frame loop timing to get back</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object (true if load completed. false if <see cref="Renderable"/> is dead.)</returns>
        public static UniTask<bool> WaitLoaded(this Renderable source, FrameLoopTiming timing = FrameLoopTiming.Update, CancellationToken cancellationToken = default)
        {
            if(source is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(source));
            }

            if(source.LifeState == LifeState.New) {
                ThrowNotActivated();
                [DoesNotReturn] static void ThrowNotActivated() => throw new ArgumentException($"{nameof(source)} is not activated.");
            }

            timing.ThrowArgExceptionIfNotSpecified(nameof(timing));

            if(cancellationToken.IsCancellationRequested) {
                return UniTask.FromCanceled<bool>(cancellationToken);
            }

            // [NOTE] See class comment of state transition

            if(source.TryGetHostScreen(out var screen)) {
                if(source.IsLoaded && screen.CurrentTiming.TimingEquals(timing)) {
                    // here is main thread
                    return new(true);
                }
                else {
                    // No one knonws the thread of here.
                    return Wait(source, screen.AsyncBack, timing, cancellationToken);
                }
            }
            else {
                // No one knonws the thread of here.
                Debug.Assert(source.LifeState == LifeState.Dead);
                // Ignore timing to get back because no one knows the screen the object belongs to.
                return new(false);
            }

            static async UniTask<bool> Wait(Renderable renderable, AsyncBackEndPoint asyncBack, FrameLoopTiming timing, CancellationToken cancellationToken)
            {
                while(true) {
                    await asyncBack.ToTiming(timing, cancellationToken);
                    if(renderable.LifeState == LifeState.Dead) {
                        Debug.Assert(renderable.IsLoaded == false);
                        return false;
                    }
                    else if(renderable.IsLoaded) {
                        return true;
                    }
                }
            }
        }
    }
}
