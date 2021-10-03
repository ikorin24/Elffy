#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy
{
    public enum FrameTiming : byte
    {
        // All values of `FrameTiming` can be cast to `ScreenCurrentTiming` except NotSpecified. (as a same name)
        NotSpecified = 0,

        EarlyUpdate = 1,
        Update = 2,
        LateUpdate = 3,
        BeforeRendering = 4,
        AfterRendering = 5,
    }

//    [DebuggerDisplay("{GetTimingName(),nq}")]
//    public readonly struct FrameTiming2 : IEquatable<FrameTiming2>
//    {
//        // TODO: Serialized as name string

//        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
//        private readonly byte _value;

//        public static FrameTiming2 NotSpecified => new(0);
//        public static FrameTiming2 EarlyUpdate => new(1);
//        public static FrameTiming2 Update => new(2);
//        public static FrameTiming2 LateUpdate => new(3);
//        public static FrameTiming2 AfterRendering => new(4);

//#if false   // for C# 10
//        [Obsolete("Don't use default constructor.", true)]
//        public FrameTiming2()
//        {
//            throw new NotSupportedException("Don't use default constructor.");
//        }
//#endif

//        private FrameTiming2(byte value) => _value = value;

//        private string GetTimingName()
//        {
//            return "";
//        }

//        public override string ToString() => GetTimingName();

//        public override bool Equals(object? obj) => obj is FrameTiming2 timing && Equals(timing);

//        public bool Equals(FrameTiming2 other) => _value == other._value;

//        public override int GetHashCode() => _value.GetHashCode();

//        public static bool operator ==(FrameTiming2 left, FrameTiming2 right) => left.Equals(right);

//        public static bool operator !=(FrameTiming2 left, FrameTiming2 right) => !(left == right);
//    }

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

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class AllowNotSpecifiedTimingAttribute : Attribute
    {
        // TODO: analyzer
    }

    public static class FrameTimingExtension
    {
        private const string DefaultMessage_TimingNotSpecified = "The timing must be specified.";
        private const string DefaultMessage_InvalidTiming = "The timing is invalid.";
        private const byte MaxValue = 5;    // max value of FrameTiming

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsSpecified(this FrameTiming source)
        {
            return source != FrameTiming.NotSpecified && (byte)source <= MaxValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsValid(this FrameTiming source)
        {
            return (byte)source <= MaxValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThrowArgExceptionIfNotSpecified(this FrameTiming source, string msg = DefaultMessage_TimingNotSpecified)
        {
            if(!source.IsSpecified()) {
                Throw(msg);
                [DoesNotReturn] static void Throw(string msg) => throw new ArgumentException(msg);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void ThrowArgExceptionIfInvalid(this FrameTiming source, string msg = DefaultMessage_InvalidTiming)
        {
            if(!source.IsValid()) {
                Throw(msg);
                [DoesNotReturn] static void Throw(string msg) => throw new ArgumentException(msg);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TimingEquals(this FrameTiming source, ScreenCurrentTiming timing)
        {
            return source == (FrameTiming)timing;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TimingEquals(this ScreenCurrentTiming source, FrameTiming timing)
        {
            return source == (ScreenCurrentTiming)timing;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ScreenCurrentTiming ToCurrentTiming(this FrameTiming timing)
        {
            return (ScreenCurrentTiming)timing;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static FrameTiming ToFrameTiming(this ScreenCurrentTiming timing)
        {
            return (FrameTiming)timing;
        }
    }
}
