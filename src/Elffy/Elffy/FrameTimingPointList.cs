#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy
{
    public sealed class FrameTimingPointList
    {
        private readonly IHostScreen _screen;
        private readonly FrameTimingPoint _earlyUpdatePoint;
        private readonly FrameTimingPoint _updatePoint;
        private readonly FrameTimingPoint _lateUpdatePoint;
        private readonly FrameTimingPoint _beforeRenderingPoint;
        private readonly FrameTimingPoint _afterRenderingPoint;

        internal IHostScreen Screen => _screen;

        public FrameTimingPoint EarlyUpdate => _earlyUpdatePoint;
        public FrameTimingPoint Update => _updatePoint;
        public FrameTimingPoint LateUpdate => _lateUpdatePoint;
        public FrameTimingPoint BeforeRendering => _beforeRenderingPoint;
        public FrameTimingPoint AfterRendering => _afterRenderingPoint;

        internal FrameTimingPointList(IHostScreen screen)
        {
            _screen = screen;
            _earlyUpdatePoint = new FrameTimingPoint(_screen, FrameTiming.EarlyUpdate);
            _updatePoint = new FrameTimingPoint(_screen, FrameTiming.Update);
            _lateUpdatePoint = new FrameTimingPoint(_screen, FrameTiming.LateUpdate);
            _beforeRenderingPoint = new FrameTimingPoint(_screen, FrameTiming.BeforeRendering);
            _afterRenderingPoint = new FrameTimingPoint(_screen, FrameTiming.AfterRendering);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FrameTimingPoint TimingOf(FrameTiming timing)
        {
            if(TryGetTimingOf(timing, out var timingPoint) == false) {
                ThrowTimingNotSpecified();
            }
            return timingPoint;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetTimingOf(FrameTiming timing, [MaybeNullWhen(false)] out FrameTimingPoint timingPoint)
        {
            if(timing == FrameTiming.EarlyUpdate) {
                timingPoint = _earlyUpdatePoint;
                return true;
            }
            else if(timing == FrameTiming.Update) {
                timingPoint = _updatePoint;
                return true;
            }
            else if(timing == FrameTiming.LateUpdate) {
                timingPoint = _lateUpdatePoint;
                return true;
            }
            else if(timing == FrameTiming.BeforeRendering) {
                timingPoint = _beforeRenderingPoint;
                return true;
            }
            else if(timing == FrameTiming.AfterRendering) {
                timingPoint = _afterRenderingPoint;
                return true;
            }
            else {
                Debug.Assert(timing.IsSpecified() == false);
                timingPoint = null;
                return false;
            }
        }

        /// <summary>Abort all suspended tasks by clearing the queue.</summary>
        internal void AbortAllEvents()
        {
            _earlyUpdatePoint.AbortAllEvents();
            _updatePoint.AbortAllEvents();
            _lateUpdatePoint.AbortAllEvents();
            _beforeRenderingPoint.AbortAllEvents();
            _afterRenderingPoint.AbortAllEvents();
        }

        [DoesNotReturn]
        private static void ThrowTimingNotSpecified() => throw new ArgumentException("The timing must be specified.");
    }
}
