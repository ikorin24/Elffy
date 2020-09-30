#nullable enable
using System;
using Cysharp.Threading.Tasks;
using Elffy.Games;

namespace Elffy.Threading.Tasks
{
    /// <summary>Helper class of short cut to <see cref="Game.AsyncBack"/></summary>
    public static class GameAsync
    {
        /// <summary>Wait for the next update timing.</summary>
        /// <returns>awaitable object</returns>
        public static FrameLoopAwaitable ToUpdate()
        {
            return Game.AsyncBack.ToFrameLoopEvent(FrameLoopTiming.Update);
        }

        /// <summary>Wait for the next early update timing.</summary>
        /// <returns>awaitable object</returns>
        public static FrameLoopAwaitable ToEarlyUpdate()
        {
            return Game.AsyncBack.ToFrameLoopEvent(FrameLoopTiming.EarlyUpdate);
        }

        /// <summary>Wait for the next late update timing.</summary>
        /// <returns>awaitable object</returns>
        public static FrameLoopAwaitable ToLateUpdate()
        {
            return Game.AsyncBack.ToFrameLoopEvent(FrameLoopTiming.LateUpdate);
        }

        /// <summary>Wait for the specified number of frames.</summary>
        /// <param name="frameCount">number for frames</param>
        /// <param name="timing">timing of end point</param>
        /// <returns>awaitable object</returns>
        public static async UniTask DelayFrame(int frameCount, FrameLoopTiming timing = FrameLoopTiming.Update)
        {
            if(frameCount < 0) {
                throw new ArgumentOutOfRangeException(nameof(frameCount));
            }
            for(int i = 0; i < frameCount; i++) {
                await ToFrameLoopTiming(timing);
            }
        }

        /// <summary>Wait for the specified time span.</summary>
        /// <param name="time">time span for waiting</param>
        /// <param name="timing">timing of end point</param>
        /// <returns>awaitable object</returns>
        public static async UniTask DelayTime(TimeSpan time, FrameLoopTiming timing = FrameLoopTiming.Update)
        {
            var start = Game.Screen.Time;
            while(Game.Screen.Time - start < time) {
                await ToFrameLoopTiming(timing);
            }
        }

        /// <summary>Wait for the specified next frame loop timing.</summary>
        /// <param name="timing">frame loop timing for waiting</param>
        /// <returns>awaitable object</returns>
        public static FrameLoopAwaitable ToFrameLoopTiming(FrameLoopTiming timing)
        {
            return Game.AsyncBack.ToFrameLoopEvent(timing, default);
        }

        public static SwitchToThreadPoolAwaitable ToThreadPool()
        {
            return UniTask.SwitchToThreadPool();
        }
    }
}
