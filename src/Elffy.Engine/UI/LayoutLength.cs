#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Elffy.Markup;
using RP = Elffy.Markup.RegexPatterns;

namespace Elffy.UI
{
    [DebuggerDisplay("{ToString(),nq}")]
    [UseLiteralMarkup]
    [LiteralMarkupPattern(LengthPattern, LengthEmit)]
    [LiteralMarkupPattern(ProportionPattern, ProportionEmit)]
    [LiteralMarkupPattern(ProportionPattern2, ProportionEmit2)]
    public readonly struct LayoutLength : IEquatable<LayoutLength>
    {
        private const string LengthPattern = @$"^(?<n>{RP.Int})$";
        private const string LengthEmit = @"new global::Elffy.UI.LayoutLength((int)(${n}), global::Elffy.UI.LayoutLengthType.Length)";
        private const string ProportionPattern = @$"^(?<n>{RP.Float})\*$";
        private const string ProportionEmit = @"new global::Elffy.UI.LayoutLength((float)(${n}), global::Elffy.UI.LayoutLengthType.Proportion)";
        private const string ProportionPattern2 = @"^\*$";
        private const string ProportionEmit2 = @"new global::Elffy.UI.LayoutLength(1f, global::Elffy.UI.LayoutLengthType.Proportion)";

        internal const string MatchPattern = $@"({RP.Int}|{RP.Float}?\*)";

        public readonly float Value;
        public readonly LayoutLengthType Type;

        public LayoutLength(float value, LayoutLengthType type)
        {
            if(value < 0) {
                ThrowOutOfRange();
            }
            Value = value;
            Type = type;

            [DoesNotReturn] static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(value));
        }

        public static LayoutLength Length(float length) => new LayoutLength(length, LayoutLengthType.Length);

        public static LayoutLength Proportion(float proportion) => new LayoutLength(proportion, LayoutLengthType.Proportion);

        public override bool Equals(object? obj) => obj is LayoutLength length && Equals(length);

        public bool Equals(LayoutLength other) => Value == other.Value && Type == other.Type;

        public override int GetHashCode() => HashCode.Combine(Value, Type);

        public override string ToString()
        {
            if(Type == LayoutLengthType.Length) {
                return Value.ToString();
            }
            else if(Type == LayoutLengthType.Proportion) {
                return Value.ToString("0.000 %");
            }
            else {
                return $"{Value} (Invalid Type)";
            }
        }

        public static bool operator ==(LayoutLength left, LayoutLength right) => left.Equals(right);

        public static bool operator !=(LayoutLength left, LayoutLength right) => !(left == right);

        public static implicit operator LayoutLength(float value) => new LayoutLength(value, LayoutLengthType.Length);
        public static implicit operator LayoutLength(int value) => new LayoutLength(value, LayoutLengthType.Length);
    }

    public enum LayoutLengthType : byte
    {
        Length,
        Proportion,
    }
}
