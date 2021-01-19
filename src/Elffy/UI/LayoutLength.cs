#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.UI
{
    public readonly struct LayoutLength : IEquatable<LayoutLength>
    {
        public readonly float Value;
        public readonly LayoutLengthType Type;

        public LayoutLength(float length)
        {
            if(length < 0) {
                ThrowOutOfRange();
            }
            Value = length;
            Type = LayoutLengthType.Length;

            [DoesNotReturn] static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(length));
        }

        public LayoutLength(float value, LayoutLengthType type)
        {
            if(value < 0) {
                ThrowOutOfRange();
            }
            Value = value;
            Type = type;

            [DoesNotReturn] static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(value));
        }

        public static LayoutLength CreateLength(float length) => new LayoutLength(length);

        public static LayoutLength CreateProportion(float proportion) => new LayoutLength(proportion, LayoutLengthType.Proportion);

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
    }

    public enum LayoutLengthType : byte
    {
        Length,
        Proportion,
    }
}
