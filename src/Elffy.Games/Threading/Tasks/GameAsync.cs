#nullable enable
using System.Threading;
using Cysharp.Threading.Tasks;
using Elffy.Games;

namespace Elffy.Threading.Tasks
{
    /// <summary>Helper class of short cut to <see cref="Game.AsyncBack"/></summary>
    public static class GameAsync
    {
        /// <summary>Switch to next update timing by using 'await'.</summary>
        /// <returns>awaitable object</returns>
        public static FrameLoopAwaitable ToUpdate()
        {
            return Game.AsyncBack.ToFrameLoopEvent(FrameLoopTiming.Update);
        }

        public static FrameLoopAwaitable ToEarlyUpdate()
        {
            return Game.AsyncBack.ToFrameLoopEvent(FrameLoopTiming.EarlyUpdate);
        }

        public static FrameLoopAwaitable ToLateUpdate()
        {
            return Game.AsyncBack.ToFrameLoopEvent(FrameLoopTiming.LateUpdate);
        }

        public static FrameLoopAwaitable ToFrameLoopEvent(FrameLoopTiming eventType, CancellationToken cancellationToken = default)
        {
            return Game.AsyncBack.ToFrameLoopEvent(eventType, cancellationToken);
        }

        public static SwitchToThreadPoolAwaitable ToThreadPool()
        {
            return UniTask.SwitchToThreadPool();
        }
    }
}
