#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy
{
    public enum FrameLoopTiming : byte
    {
        // All values of `FrameLoopTiming` can be cast to `ScreenCurrentTiming` except NotSpecified. (as a same name)
        NotSpecified = 0,

        EarlyUpdate = 1,
        Update = 2,
        LateUpdate = 3,
        BeforeRendering = 4,
        AfterRendering = 5,
    }

    public enum ScreenCurrentTiming : byte
    {
        OutOfFrameLoop = 0, // default value must be 'OutOfFrameLoop'

        EarlyUpdate = 1,
        Update = 2,
        LateUpdate = 3,
        BeforeRendering = 4,
        AfterRendering = 5,

        FrameInitializing = byte.MaxValue,
        FrameFinalizing = byte.MaxValue - 1,
    }

    public static class FrameLoopTimingExtension
    {
        private const string DefaultMessage_TimingNotSpecified = "The timing must be specified.";
        private const byte MaxValue = 5;    // max value of FrameLoopTiming

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsSpecified(this FrameLoopTiming source)
        {
            return source != FrameLoopTiming.NotSpecified && (byte)source <= MaxValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsValid(this FrameLoopTiming source)
        {
            return (byte)source <= MaxValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThrowArgExceptionIfNotSpecified(this FrameLoopTiming source, string msg = DefaultMessage_TimingNotSpecified)
        {
            if(!source.IsSpecified()) {
                Throw(msg);
                [DoesNotReturn] static void Throw(string msg) => throw new ArgumentException(msg);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TimingEquals(this FrameLoopTiming source, ScreenCurrentTiming timing)
        {
            return source == (FrameLoopTiming)timing;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TimingEquals(this ScreenCurrentTiming source, FrameLoopTiming timing)
        {
            return source == (ScreenCurrentTiming)timing;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ScreenCurrentTiming ToCurrentTiming(this FrameLoopTiming timing)
        {
            return (ScreenCurrentTiming)timing;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static FrameLoopTiming ToLoopTiming(this ScreenCurrentTiming timing)
        {
            return (FrameLoopTiming)timing;
        }
    }
}
