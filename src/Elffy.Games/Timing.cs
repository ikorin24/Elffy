﻿#nullable enable
using System;
using System.Diagnostics;
using System.Threading;
using System.Runtime.CompilerServices;
using System.ComponentModel;
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

        internal static AsyncBackEndPoint EndPoint => _endPoint!;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Initialize(IHostScreen screen)
        {
            Debug.Assert(_endPoint is null);
            _screen = screen;
            _endPoint = screen.AsyncBack;
        }

        /// <summary>Get current screen frame loop timing.</summary>
        /// <remarks>If not main thread of <see cref="IHostScreen"/>, always returns <see cref="ScreenCurrentTiming.OutOfFrameLoop"/></remarks>
        public static ScreenCurrentTiming CurrentTiming => _screen!.CurrentTiming;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FrameAsyncEnumerable Frames(FrameLoopTiming timing = FrameLoopTiming.Update, CancellationToken cancellationToken = default)
        {
            return _screen!.Frames(timing, cancellationToken);
        }

        /// <summary>Wait for specified timing if current timing is not the timing. Otherwise, do nothing.</summary>
        /// <param name="timing">timing to wait</param>
        /// <param name="cancellation">cancellation token</param>
        /// <returns>awaitable object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<AsyncUnit> Ensure(FrameLoopTiming timing, CancellationToken cancellation = default)
        {
            return _endPoint!.Ensure(timing, cancellation);
        }

        /// <summary>Wait for specified timing.</summary>
        /// <param name="timing">timing to wait</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<AsyncUnit> ToTiming(FrameLoopTiming timing, CancellationToken cancellationToken = default)
        {
            return _endPoint!.ToTiming(timing, cancellationToken);
        }

        /// <summary>Wait for the next update timing.</summary>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<AsyncUnit> ToUpdate(CancellationToken cancellationToken = default)
        {
            return _endPoint!.ToTiming(FrameLoopTiming.Update, cancellationToken);
        }

        /// <summary>Wait for the next early update timing.</summary>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<AsyncUnit> ToEarlyUpdate(CancellationToken cancellationToken = default)
        {
            return _endPoint!.ToTiming(FrameLoopTiming.EarlyUpdate, cancellationToken);
        }

        /// <summary>Wait for the next late update timing.</summary>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<AsyncUnit> ToLateUpdate(CancellationToken cancellationToken = default)
        {
            return _endPoint!.ToTiming(FrameLoopTiming.LateUpdate, cancellationToken);
        }

        /// <summary>Wait for the next before rendering timing.</summary>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<AsyncUnit> ToBeforeRendering(CancellationToken cancellationToken = default)
        {
            return _endPoint!.ToTiming(FrameLoopTiming.BeforeRendering, cancellationToken);
        }

        /// <summary>Wait for the next after rendering timing.</summary>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<AsyncUnit> ToAfterRendering(CancellationToken cancellationToken = default)
        {
            return _endPoint!.ToTiming(FrameLoopTiming.AfterRendering, cancellationToken);
        }

        /// <summary>Wait for the specified number of frames.</summary>
        /// <param name="frameCount">number for frames</param>
        /// <param name="timing">timing of end point</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async UniTask<AsyncUnit> DelayFrame(int frameCount, FrameLoopTiming timing = FrameLoopTiming.Update, CancellationToken cancellationToken = default)
        {
            if(frameCount < 0) {
                ThrowOutOfRange();
                static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(frameCount));
            }
            for(int i = 0; i < frameCount; i++) {
                await ToTiming(timing, cancellationToken);
            }
            return AsyncUnit.Default;
        }

        /// <summary>Wait for the specified time.</summary>
        /// <param name="time">time span for waiting</param>
        /// <param name="timing">timing of end point</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async UniTask<AsyncUnit> DelayTime(TimeSpan time, FrameLoopTiming timing = FrameLoopTiming.Update, CancellationToken cancellationToken = default)
        {
            var start = Game.Time;
            while(Game.Time - start <= time) {
                await ToTiming(timing, cancellationToken);
            }
            return AsyncUnit.Default;
        }

        /// <summary>Wait for the specified time.</summary>
        /// <param name="millisecond">millisecond time for waiting</param>
        /// <param name="timing">timing of end point</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<AsyncUnit> DelayTime(int millisecond, FrameLoopTiming timing = FrameLoopTiming.Update, CancellationToken cancellationToken = default)
        {
            return DelayTime(TimeSpan.FromMilliseconds(millisecond), timing, cancellationToken);
        }

        /// <summary>Wait for the specified real time.</summary>
        /// <param name="time">time span for waiting</param>
        /// <param name="timing">timing of end point</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async UniTask<AsyncUnit> DelayRealTime(TimeSpan time, FrameLoopTiming timing = FrameLoopTiming.Update, CancellationToken cancellationToken = default)
        {
            var start = Engine.RunningRealTime;
            while(Engine.RunningRealTime - start <= time) {
                await ToTiming(timing, cancellationToken);
            }
            return AsyncUnit.Default;
        }

        /// <summary>Wait for the specified real time.</summary>
        /// <param name="millisecond">millisecond time for waiting</param>
        /// <param name="timing">timing of end point</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns>awaitable object</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UniTask<AsyncUnit> DelayRealTime(int millisecond, FrameLoopTiming timing = FrameLoopTiming.Update, CancellationToken cancellationToken = default)
        {
            return DelayRealTime(TimeSpan.FromMilliseconds(millisecond), timing, cancellationToken);
        }
    }
}
