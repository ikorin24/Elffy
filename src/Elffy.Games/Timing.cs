#nullable enable
using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;

namespace Elffy
{
    // In the case that only one IHostScreen instance exists,
    // you can avoid accessing the property via interface and do easily by using this class.

    /// <summary>Provides awaitable timings of <see cref="IHostScreen"/></summary>
    /// <remarks>This class is a shortcut to <see cref="IHostScreen.AsyncBack"/></remarks>
    public static class Timing
    {
        // I don't check null for performance.
        // So the user MUST call Initialize() before using this class.
        private static IHostScreen? _screen;
        private static AsyncBackEndPoint? _endPoint;

        internal static void Initialize(IHostScreen screen)
        {
            Debug.Assert(_endPoint is null);
            _screen = screen;
            _endPoint = screen.AsyncBack;
        }

        /// <summary>Get current screen frame loop timing.</summary>
        /// <remarks>If not main thread of <see cref="IHostScreen"/>, always returns <see cref="ScreenCurrentTiming.OutOfFrameLoop"/></remarks>
        public static ScreenCurrentTiming CurrentTiming => _screen!.CurrentTiming;

        /// <summary>Wait for specified timing.</summary>
        /// <param name="timing">timing to wait</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FrameLoopAwaitable ToFrameLoopTiming(FrameLoopTiming timing, CancellationToken cancellationToken = default)
        {
            return _endPoint!.ToFrameLoopEvent(timing, cancellationToken);
        }

        /// <summary>Wait for the next update timing.</summary>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FrameLoopAwaitable ToUpdate(CancellationToken cancellationToken = default)
        {
            return _endPoint!.ToFrameLoopEvent(FrameLoopTiming.Update, cancellationToken);
        }

        /// <summary>Wait for the next early update timing.</summary>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FrameLoopAwaitable ToEarlyUpdate(CancellationToken cancellationToken = default)
        {
            return _endPoint!.ToFrameLoopEvent(FrameLoopTiming.EarlyUpdate, cancellationToken);
        }

        /// <summary>Wait for the next late update timing.</summary>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FrameLoopAwaitable ToLateUpdate(CancellationToken cancellationToken = default)
        {
            return _endPoint!.ToFrameLoopEvent(FrameLoopTiming.LateUpdate, cancellationToken);
        }

        /// <summary>Wait for the next before rendering timing.</summary>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FrameLoopAwaitable ToBeforeRendering(CancellationToken cancellationToken = default)
        {
            return _endPoint!.ToFrameLoopEvent(FrameLoopTiming.BeforeRendering, cancellationToken);
        }

        /// <summary>Wait for the next after rendering timing.</summary>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FrameLoopAwaitable ToAfterRendering(CancellationToken cancellationToken = default)
        {
            return _endPoint!.ToFrameLoopEvent(FrameLoopTiming.AfterRendering, cancellationToken);
        }

        /// <summary>Wait for the specified number of frames.</summary>
        /// <param name="frameCount">number for frames</param>
        /// <param name="timing">timing of end point</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async UniTask DelayFrame(int frameCount, FrameLoopTiming timing = FrameLoopTiming.Update, CancellationToken cancellationToken = default)
        {
            if(frameCount < 0) {
                ThrowOutOfRange();
                static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(frameCount));
            }
            for(int i = 0; i < frameCount; i++) {
                await ToFrameLoopTiming(timing, cancellationToken);
            }
        }

        /// <summary>Wait for the specified time span.</summary>
        /// <param name="time">time span for waiting</param>
        /// <param name="timing">timing of end point</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async UniTask DelayTime(TimeSpan time, FrameLoopTiming timing = FrameLoopTiming.Update, CancellationToken cancellationToken = default)
        {
            var start = Game.Time;
            while(Game.Time - start < time) {
                await ToFrameLoopTiming(timing, cancellationToken);
            }
        }
    }
}
