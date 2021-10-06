#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy
{
    [GenerateEnumLikeStruct(typeof(byte))]
    [EnumLikeValue("NotSpecified", 0)]
    [EnumLikeValue("EarlyUpdate", 1)]
    [EnumLikeValue("Update", 2)]
    [EnumLikeValue("LateUpdate", 3)]
    [EnumLikeValue("BeforeRendering", 4)]
    [EnumLikeValue("AfterRendering", 5)]
    public partial struct FrameTiming
    {
        private const string DefaultMessage_TimingNotSpecified = "The timing must be specified.";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsSpecified() => this != NotSpecified;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsValid() => _value <= 5;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ThrowArgExceptionIfNotSpecified(string msg = DefaultMessage_TimingNotSpecified)
        {
            if(!IsSpecified()) {
                Throw(msg);
                [DoesNotReturn] static void Throw(string msg) => throw new ArgumentException(msg);
            }
        }

        public static bool operator ==(CurrentFrameTiming left, FrameTiming right)
        {
            Debug.Assert(Unsafe.SizeOf<CurrentFrameTiming>() == sizeof(byte));
            Debug.Assert(Unsafe.SizeOf<FrameTiming>() == sizeof(byte));

            var l = Unsafe.As<CurrentFrameTiming, FrameTiming>(ref left);
            return (l._value != 0) && (l._value == right._value);
        }

        public static bool operator !=(CurrentFrameTiming left, FrameTiming right) => !(left == right);

        public static bool operator ==(FrameTiming left, CurrentFrameTiming right) => right == left;

        public static bool operator !=(FrameTiming left, CurrentFrameTiming right) => !(left == right);
    }
}
