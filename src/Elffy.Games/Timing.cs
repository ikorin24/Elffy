#nullable enable
using System.Diagnostics;
using System.ComponentModel;

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

        public static FrameTimingPoint EarlyUpdate => EndPoint.EarlyUpdate;
        public static FrameTimingPoint Update => EndPoint.Update;
        public static FrameTimingPoint LateUpdate => EndPoint.LateUpdate;
        public static FrameTimingPoint BeforeRendering => EndPoint.BeforeRendering;
        public static FrameTimingPoint AfterRendering => EndPoint.AfterRendering;

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
    }
}
