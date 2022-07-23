#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy
{
    public readonly struct CoroutineState : IEquatable<CoroutineState>
    {
        private readonly FrameObject? _frameObject;
        private readonly FrameTimingPointList _timingPoints;
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
        public FrameTimingPointList TimingPoints => _timingPoints;

        public FrameTimingPoint EarlyUpdate => _timingPoints.EarlyUpdate;
        public FrameTimingPoint Update => _timingPoints.Update;
        public FrameTimingPoint LateUpdate => _timingPoints.LateUpdate;
        public FrameTimingPoint BeforeRendering => _timingPoints.BeforeRendering;
        public FrameTimingPoint AfterRendering => _timingPoints.AfterRendering;

        internal CoroutineState(IHostScreen screen)
        {
            Debug.Assert(screen is not null);
            _frameObject = null;
            _screen = screen;
            _timingPoints = screen.Timings;
        }

        internal CoroutineState(FrameObject frameObject)
        {
            Debug.Assert(frameObject is not null);
            Debug.Assert(frameObject.LifeState.IsSameOrAfter(LifeState.Alive));
            var hasScreen = frameObject.TryGetScreen(out var screen);
            Debug.Assert(hasScreen);
            Debug.Assert(screen is not null);
            _frameObject = frameObject;
            _screen = screen;
            _timingPoints = screen.Timings;
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

        public FrameTimingPoint TimingOf(FrameTiming timing) => _timingPoints.GetTiming(timing);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CoroutineFrameAsyncEnumerable Frames(FrameTiming timing)
        {
            timing.ThrowArgExceptionIfNotSpecified(nameof(timing));
            return new CoroutineFrameAsyncEnumerable(this, timing);
        }

        public override bool Equals(object? obj) => obj is CoroutineState state && Equals(state);

        public bool Equals(CoroutineState other) => _frameObject == other._frameObject &&
                                                    _timingPoints == other._timingPoints &&
                                                    _screen == other._screen;

        public override int GetHashCode() => HashCode.Combine(_frameObject, _timingPoints, _screen);
    }
}
