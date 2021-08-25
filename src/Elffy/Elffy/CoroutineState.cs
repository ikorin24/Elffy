#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy
{
    public readonly struct CoroutineState : IEquatable<CoroutineState>
    {
        private readonly FrameObject? _frameObject;
        private readonly AsyncBackEndPoint _endPoint;
        private readonly IHostScreen _screen;

        /// <summary>Get whether the coroutine can continue running.</summary>
        /// <remarks>If the property is false, you must finish the coroutine method.</remarks>
        public bool CanRun
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => CoroutineCanRun(_screen, _frameObject);
        }

        internal FrameObject? FrameObject => _frameObject;
        public IHostScreen Screen => _screen;
        public AsyncBackEndPoint AsyncEndPoint => _endPoint;

        internal CoroutineState(IHostScreen screen)
        {
            Debug.Assert(screen is not null);
            _frameObject = null;
            _screen = screen;
            _endPoint = screen.AsyncBack;
        }

        internal CoroutineState(FrameObject frameObject)
        {
            Debug.Assert(frameObject is not null);
            Debug.Assert(frameObject.LifeState.IsSameOrAfter(LifeState.Activated));
            var hasScreen = frameObject.TryGetHostScreen(out var screen);
            Debug.Assert(hasScreen);
            Debug.Assert(screen is not null);
            _frameObject = frameObject;
            _screen = screen;
            _endPoint = screen.AsyncBack;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool CoroutineCanRun(IHostScreen screen, FrameObject? frameObject)
        {
            if(frameObject is null) {
                return screen.IsRunning;
            }
            else {
                return screen.IsRunning && frameObject.LifeState.IsBefore(LifeState.Dead);
            }
        }

        public UniTask<AsyncUnit> ToTiming(FrameLoopTiming timing) => _endPoint.ToTiming(timing);
        public UniTask<AsyncUnit> ToEarlyUpdate() => _endPoint.ToTiming(FrameLoopTiming.EarlyUpdate);
        public UniTask<AsyncUnit> ToUpdate() => _endPoint.ToTiming(FrameLoopTiming.Update);
        public UniTask<AsyncUnit> ToLateUpdate() => _endPoint.ToTiming(FrameLoopTiming.LateUpdate);
        public UniTask<AsyncUnit> ToBeforeRendering() => _endPoint.ToTiming(FrameLoopTiming.BeforeRendering);
        public UniTask<AsyncUnit> ToAfterRendering() => _endPoint.ToTiming(FrameLoopTiming.AfterRendering);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CoroutineFrameAsyncEnumerable Frames(FrameLoopTiming timing = FrameLoopTiming.Update)
        {
            timing.ThrowArgExceptionIfNotSpecified(nameof(timing));
            return new CoroutineFrameAsyncEnumerable(this, timing);
        }

        public override bool Equals(object? obj) => obj is CoroutineState state && Equals(state);

        public bool Equals(CoroutineState other) => _frameObject == other._frameObject &&
                                                    _endPoint == other._endPoint &&
                                                    _screen == other._screen;

        public override int GetHashCode() => HashCode.Combine(_frameObject, _endPoint, _screen);
    }
}
