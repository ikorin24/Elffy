#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Elffy
{
    [DebuggerDisplay("{ToString(),nq}")]
    public readonly struct FrameTiming : IEquatable<FrameTiming>
    {
        private const string DefaultMessage_TimingNotSpecified = "The timing must be specified.";

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly byte _value;

        public static FrameTiming NotSpecified => new((byte)0);
        public static FrameTiming EarlyUpdate => new(1);
        public static FrameTiming Update => new(2);
        public static FrameTiming LateUpdate => new(3);
        public static FrameTiming BeforeRendering => new(4);
        public static FrameTiming AfterRendering => new(5);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly FrameTiming[] _allValues = new FrameTiming[]
        {
            NotSpecified,
            EarlyUpdate,
            Update,
            LateUpdate,
            BeforeRendering,
            AfterRendering,
        };

#if false   // for C# 10
        [Obsolete("Don't use default constructor.", true)]
        public FrameTiming()
        {
            throw new NotSupportedException("Don't use default constructor.");
        }
#endif

        private FrameTiming(byte value) => _value = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryCreateFrom(CurrentFrameTiming currentTiming, out FrameTiming frameTiming)
        {
            var value = currentTiming.GetInnerValue();
            if(TryCreateInstance(value, out frameTiming)) {
                return true;
            }
            else {
                frameTiming = NotSpecified;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsSpecified() => this != NotSpecified;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsValid() => TryCreateInstance(_value, out _);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ThrowArgExceptionIfNotSpecified(string msg = DefaultMessage_TimingNotSpecified)
        {
            if(!IsSpecified()) {
                Throw(msg);
                [DoesNotReturn] static void Throw(string msg) => throw new ArgumentException(msg);
            }
        }

        private static bool TryCreateInstance(byte value, out FrameTiming instance)
        {
            if(value == 0) {
                instance = new((byte)0);
                return true;
            }
            else if(value == 1) {
                instance = new((byte)1);
                return true;
            }
            else if(value == 2) {
                instance = new((byte)2);
                return true;
            }
            else if(value == 3) {
                instance = new((byte)3);
                return true;
            }
            else if(value == 4) {
                instance = new((byte)4);
                return true;
            }
            else if(value == 5) {
                instance = new((byte)5);
                return true;
            }
            else {
                instance = default;
                return false;
            }
        }

        public static ReadOnlySpan<FrameTiming> AllValues() => _allValues;
        public static IEnumerable<FrameTiming> AllValuesEnumerable() => _allValues;

        public override string ToString()
        {
            if(this == NotSpecified) {
                return "NotSpecified";
            }
            else if(this == EarlyUpdate) {
                return "EarlyUpdate";
            }
            else if(this == Update) {
                return "Update";
            }
            else if(this == LateUpdate) {
                return "LateUpdate";
            }
            else if(this == BeforeRendering) {
                return "BeforeRendering";
            }
            else if(this == AfterRendering) {
                return "AfterRendering";
            }
            else {
                return _value.ToString();
            }
        }

        public override bool Equals(object? obj) => obj is FrameTiming timing && Equals(timing);

        public bool Equals(FrameTiming other) => _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public static bool operator ==(FrameTiming left, FrameTiming right) => left.Equals(right);

        public static bool operator !=(FrameTiming left, FrameTiming right) => !(left == right);

        public static bool operator ==(CurrentFrameTiming left, FrameTiming right) => TryCreateFrom(left, out var t) && t == right;

        public static bool operator !=(CurrentFrameTiming left, FrameTiming right) => !(left == right);

        public static bool operator ==(FrameTiming left, CurrentFrameTiming right) => TryCreateFrom(right, out var t) && left == t;

        public static bool operator !=(FrameTiming left, CurrentFrameTiming right) => !(left == right);
    }
}
