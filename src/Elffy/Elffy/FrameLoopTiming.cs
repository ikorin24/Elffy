#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy
{
    public enum FrameLoopTiming : byte
    {
        // All values of `FrameLoopTiming` can be cast to `ScreenCurrentTiming`. (as a same name)

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
        FrameFinalizing = byte.MinValue,
    }

    public static class LoopTimingExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid(this FrameLoopTiming source)
        {
            const byte MaxValue = 5;    // max value of FrameLoopTiming
            const byte Default = 0;

            return (byte)source != Default && (byte)source <= MaxValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowArgExceptionIfInvalid(this FrameLoopTiming source, string msg)
        {
            if(!source.IsValid()) {
                Throw(msg);
                [DoesNotReturn] static void Throw(string msg) => throw new ArgumentException(msg);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equals(this FrameLoopTiming source, ScreenCurrentTiming timing)
        {
            return source == (FrameLoopTiming)timing;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equals(this ScreenCurrentTiming source, FrameLoopTiming timing)
        {
            return source == (ScreenCurrentTiming)timing;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScreenCurrentTiming ToCurrentTiming(this FrameLoopTiming timing)
        {
            return (ScreenCurrentTiming)timing;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FrameLoopTiming ToLoopTiming(this ScreenCurrentTiming timing)
        {
            return (FrameLoopTiming)timing;
        }
    }
}
