#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using FTV = Elffy.FrameTimingValues;

namespace Elffy
{
    [GenerateEnumLikeStruct(typeof(byte))]
    [EnumLikeValue(nameof(FTV.NotSpecified), FTV.NotSpecified)]
    [EnumLikeValue(nameof(FTV.FrameInitializing), FTV.FrameInitializing)]
    [EnumLikeValue(nameof(FTV.EarlyUpdate), FTV.EarlyUpdate)]
    [EnumLikeValue(nameof(FTV.Update), FTV.Update)]
    [EnumLikeValue(nameof(FTV.LateUpdate), FTV.LateUpdate)]
    [EnumLikeValue(nameof(FTV.BeforeRendering), FTV.BeforeRendering)]
    [EnumLikeValue(nameof(FTV.AfterRendering), FTV.AfterRendering)]
    [EnumLikeValue(nameof(FTV.FrameFinalizing), FTV.FrameFinalizing)]
    [EnumLikeValue(nameof(FTV.Internal_EndOfFrame), FTV.Internal_EndOfFrame, "internal")]
    public partial struct FrameTiming
    {
        private const string DefaultMessage_TimingNotSpecified = "The timing must be specified.";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsSpecified() => _value != FTV.NotSpecified;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ThrowArgExceptionIfNotSpecified(string msg = DefaultMessage_TimingNotSpecified)
        {
            if(!IsSpecified()) {
                Throw(msg);
                [DoesNotReturn] static void Throw(string msg) => throw new ArgumentException(msg);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(FrameTiming left, FrameTiming right)
        {
            // NotSpecified always returns false for any large/small comparison.
            if(left._value == FTV.NotSpecified || right._value == FTV.NotSpecified) {
                return false;
            }
            return left._value < right._value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(FrameTiming left, FrameTiming right)
        {
            // NotSpecified always returns false for any large/small comparison.
            if(left._value == FTV.NotSpecified || right._value == FTV.NotSpecified) {
                return false;
            }
            return left._value > right._value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(FrameTiming left, FrameTiming right)
        {
            // NotSpecified always returns false for any large/small comparison
            // except both of them are NotSpecified.
            if(left._value == FTV.NotSpecified || right._value == FTV.NotSpecified) {
                return left._value == right._value;
            }
            return left._value <= right._value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(FrameTiming left, FrameTiming right)
        {
            // NotSpecified always returns false for any large/small comparison
            // except both of them are NotSpecified.
            if(left._value == FTV.NotSpecified || right._value == FTV.NotSpecified) {
                return left._value == right._value;
            }
            return left._value >= right._value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CurrentFrameTiming ToCurrentFrameTiming()
        {
            // It is valid
            // cast FrameTiming -> CurrentFrameTiming

            Debug.Assert(Unsafe.SizeOf<CurrentFrameTiming>() == sizeof(byte));
            Debug.Assert(Unsafe.SizeOf<FrameTiming>() == sizeof(byte));

            return Unsafe.As<byte, CurrentFrameTiming>(ref Unsafe.AsRef(in _value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator CurrentFrameTiming(FrameTiming timing) => timing.ToCurrentFrameTiming();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(CurrentFrameTiming left, FrameTiming right)
        {
            Debug.Assert(Unsafe.SizeOf<CurrentFrameTiming>() == sizeof(byte));
            Debug.Assert(Unsafe.SizeOf<FrameTiming>() == sizeof(byte));

            var l = Unsafe.As<CurrentFrameTiming, FrameTiming>(ref left);
            return (l._value != 0) && (l._value == right._value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(CurrentFrameTiming left, FrameTiming right) => !(left == right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(FrameTiming left, CurrentFrameTiming right) => right == left;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(FrameTiming left, CurrentFrameTiming right) => !(left == right);
    }


    [GenerateEnumLikeStruct(typeof(byte))]
    [EnumLikeValue(nameof(FTV.OutOfFrameLoop), FTV.OutOfFrameLoop)]
    [EnumLikeValue(nameof(FTV.FirstFrameInitializing), FTV.FirstFrameInitializing)]
    [EnumLikeValue(nameof(FTV.FrameInitializing), FTV.FrameInitializing)]
    [EnumLikeValue(nameof(FTV.EarlyUpdate), FTV.EarlyUpdate)]
    [EnumLikeValue(nameof(FTV.Update), FTV.Update)]
    [EnumLikeValue(nameof(FTV.LateUpdate), FTV.LateUpdate)]
    [EnumLikeValue(nameof(FTV.BeforeRendering), FTV.BeforeRendering)]
    [EnumLikeValue(nameof(FTV.Rendering), FTV.Rendering)]
    [EnumLikeValue(nameof(FTV.AfterRendering), FTV.AfterRendering)]
    [EnumLikeValue(nameof(FTV.FrameFinalizing), FTV.FrameFinalizing)]
    [EnumLikeValue(nameof(FTV.Internal_EndOfFrame), FTV.Internal_EndOfFrame, "internal")]
    public partial struct CurrentFrameTiming
    {
        public bool IsOutOfFrameLoop() => _value == OutOfFrameLoop._value;
    }

    internal static class FrameTimingValues
    {
        internal const byte OutOfFrameLoop = 0;
        internal const byte NotSpecified = 0;
        internal const byte FirstFrameInitializing = 1;
        internal const byte FrameInitializing = 2;
        internal const byte EarlyUpdate = 3;
        internal const byte Update = 4;
        internal const byte LateUpdate = 5;
        internal const byte BeforeRendering = 6;
        internal const byte Rendering = 7;
        internal const byte AfterRendering = 8;
        internal const byte FrameFinalizing = 9;
        internal const byte Internal_EndOfFrame = 10;
    }
}
