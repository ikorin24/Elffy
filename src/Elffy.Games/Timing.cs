#nullable enable
using System.Diagnostics;
using System.ComponentModel;

namespace Elffy
{
    // In the case that only one IHostScreen instance exists,
    // you can avoid accessing the property via interface and do easily by using this class.

    /// <summary>Provides awaitable timings of <see cref="IHostScreen"/></summary>
    /// <remarks>This class is a shortcut to <see cref="IHostScreen.TimingPoints"/></remarks>
    public static class Timing
    {
        // I don't check null for performance.
        // So the user MUST call Initialize() before using this class.
        private static IHostScreen? _screen;
        private static FrameTimingPointList? _timingPoints;

        internal static FrameTimingPointList TimingPoints => _timingPoints!;

        public static FrameTimingPoint EarlyUpdate => TimingPoints.EarlyUpdate;
        public static FrameTimingPoint Update => TimingPoints.Update;
        public static FrameTimingPoint LateUpdate => TimingPoints.LateUpdate;
        public static FrameTimingPoint BeforeRendering => TimingPoints.BeforeRendering;
        public static FrameTimingPoint AfterRendering => TimingPoints.AfterRendering;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Initialize(IHostScreen screen)
        {
            Debug.Assert(_timingPoints is null);
            _screen = screen;
            _timingPoints = screen.TimingPoints;
        }

        /// <summary>Get current screen frame loop timing.</summary>
        /// <remarks>If not main thread of <see cref="IHostScreen"/>, always returns <see cref="CurrentFrameTiming.OutOfFrameLoop"/></remarks>
        public static CurrentFrameTiming CurrentTiming => _screen!.CurrentTiming;
    }
}
