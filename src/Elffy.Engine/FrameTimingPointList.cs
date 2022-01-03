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
        private readonly FrameTimingPoint _frameInitializingPoint;
        private readonly FrameTimingPoint _earlyUpdatePoint;
        private readonly FrameTimingPoint _updatePoint;
        private readonly FrameTimingPoint _lateUpdatePoint;
        private readonly FrameTimingPoint _beforeRenderingPoint;
        private readonly FrameTimingPoint _afterRenderingPoint;

        private readonly FrameTimingPoint _internalEndOfFrame;

        internal IHostScreen Screen => _screen;

        public FrameTimingPoint FrameInitializing => _frameInitializingPoint;
        public FrameTimingPoint EarlyUpdate => _earlyUpdatePoint;
        public FrameTimingPoint Update => _updatePoint;
        public FrameTimingPoint LateUpdate => _lateUpdatePoint;
        public FrameTimingPoint BeforeRendering => _beforeRenderingPoint;
        public FrameTimingPoint AfterRendering => _afterRenderingPoint;

        internal FrameTimingPoint InternalEndOfFrame => _internalEndOfFrame;

        internal FrameTimingPointList(IHostScreen screen)
        {
            _screen = screen;
            _frameInitializingPoint = new FrameTimingPoint(screen, FrameTiming.FrameInitializing);
            _earlyUpdatePoint = new FrameTimingPoint(screen, FrameTiming.EarlyUpdate);
            _updatePoint = new FrameTimingPoint(screen, FrameTiming.Update);
            _lateUpdatePoint = new FrameTimingPoint(screen, FrameTiming.LateUpdate);
            _beforeRenderingPoint = new FrameTimingPoint(screen, FrameTiming.BeforeRendering);
            _afterRenderingPoint = new FrameTimingPoint(screen, FrameTiming.AfterRendering);
            _internalEndOfFrame = new FrameTimingPoint(screen, FrameTiming.Internal_EndOfFrame);
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
            // [NOTE]
            // Only for public timing point

            if(timing == FrameTiming.FrameInitializing) {
                timingPoint = _frameInitializingPoint;
                return true;
            }
            else if(timing == FrameTiming.EarlyUpdate) {
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
                timingPoint = null;
                return false;
            }
        }

        /// <summary>Abort all suspended tasks by clearing the queue.</summary>
        internal void AbortAllEvents()
        {
            _frameInitializingPoint.AbortAllEvents();
            _earlyUpdatePoint.AbortAllEvents();
            _updatePoint.AbortAllEvents();
            _lateUpdatePoint.AbortAllEvents();
            _beforeRenderingPoint.AbortAllEvents();
            _afterRenderingPoint.AbortAllEvents();
            _internalEndOfFrame.AbortAllEvents();
        }

        [DoesNotReturn]
        private static void ThrowTimingNotSpecified() => throw new ArgumentException("The timing must be specified.");
    }
}
