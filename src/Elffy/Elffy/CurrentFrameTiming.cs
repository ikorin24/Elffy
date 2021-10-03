#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Elffy
{
    [DebuggerDisplay("{ToString(),nq}")]
    public readonly struct CurrentFrameTiming : IEquatable<CurrentFrameTiming>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly byte _value;

        public static CurrentFrameTiming OutOfFrameLoop => new((byte)0);
        public static CurrentFrameTiming EarlyUpdate => new(1);
        public static CurrentFrameTiming Update => new(2);
        public static CurrentFrameTiming LateUpdate => new(3);
        public static CurrentFrameTiming BeforeRendering => new(4);
        public static CurrentFrameTiming AfterRendering => new(5);
        public static CurrentFrameTiming FrameInitializing => new(100);
        public static CurrentFrameTiming FrameFinalizing => new(101);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly CurrentFrameTiming[] _allValues = new CurrentFrameTiming[]
        {
            OutOfFrameLoop,
            EarlyUpdate,
            Update,
            LateUpdate,
            BeforeRendering,
            AfterRendering,
            FrameInitializing,
            FrameFinalizing,
        };

#if false   // for C# 10
        [Obsolete("Don't use default constructor.", true)]
        public CurrentFrameTiming()
        {
            throw new NotSupportedException("Don't use default constructor.");
        }
#endif

        private CurrentFrameTiming(byte value) => _value = value;

        internal byte GetInnerValue() => _value;

        public static ReadOnlySpan<CurrentFrameTiming> AllValues() => _allValues;
        public static IEnumerable<CurrentFrameTiming> AllValuesEnumerable() => _allValues;

        public override string ToString()
        {
            if(this == OutOfFrameLoop) {
                return "OutOfFrameLoop";
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
            else if(this == FrameInitializing) {
                return "FrameInitializing";
            }
            else if(this == FrameFinalizing) {
                return "FrameFinalizing";
            }
            else {
                return _value.ToString();
            }
        }

        public override bool Equals(object? obj) => obj is CurrentFrameTiming timing && Equals(timing);

        public bool Equals(CurrentFrameTiming other) => _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public static bool operator ==(CurrentFrameTiming left, CurrentFrameTiming right) => left.Equals(right);

        public static bool operator !=(CurrentFrameTiming left, CurrentFrameTiming right) => !(left == right);
    }
}
