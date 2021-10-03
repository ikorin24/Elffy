#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Elffy
{
    [DebuggerDisplay("{ToString(),nq}")]
    public readonly struct ScreenCurrentTiming : IEquatable<ScreenCurrentTiming>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly byte _value;

        public static ScreenCurrentTiming OutOfFrameLoop => new((byte)0);
        public static ScreenCurrentTiming EarlyUpdate => new(1);
        public static ScreenCurrentTiming Update => new(2);
        public static ScreenCurrentTiming LateUpdate => new(3);
        public static ScreenCurrentTiming BeforeRendering => new(4);
        public static ScreenCurrentTiming AfterRendering => new(5);
        public static ScreenCurrentTiming FrameInitializing => new(100);
        public static ScreenCurrentTiming FrameFinalizing => new(101);

        private static readonly ScreenCurrentTiming[] _allValues = new ScreenCurrentTiming[]
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
        public ScreenCurrentTiming()
        {
            throw new NotSupportedException("Don't use default constructor.");
        }
#endif

        private ScreenCurrentTiming(byte value) => _value = value;

        internal byte GetInnerValue() => _value;

        public static ReadOnlySpan<ScreenCurrentTiming> AllValues() => _allValues;
        public static IEnumerable<ScreenCurrentTiming> AllValuesEnumerable() => _allValues;

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

        public override bool Equals(object? obj) => obj is ScreenCurrentTiming timing && Equals(timing);

        public bool Equals(ScreenCurrentTiming other) => _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public static bool operator ==(ScreenCurrentTiming left, ScreenCurrentTiming right) => left.Equals(right);

        public static bool operator !=(ScreenCurrentTiming left, ScreenCurrentTiming right) => !(left == right);
    }
}
