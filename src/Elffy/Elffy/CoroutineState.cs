#nullable enable
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

        public FrameTimingPoint EarlyUpdate => _endPoint.EarlyUpdate;
        public FrameTimingPoint Update => _endPoint.Update;
        public FrameTimingPoint LateUpdate => _endPoint.LateUpdate;
        public FrameTimingPoint BeforeRendering => _endPoint.BeforeRendering;
        public FrameTimingPoint AfterRendering => _endPoint.AfterRendering;

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

        public FrameTimingPoint TimingOf(FrameTiming timing) => _endPoint.TimingOf(timing);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CoroutineFrameAsyncEnumerable Frames(FrameTiming timing = FrameTiming.Update)
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
