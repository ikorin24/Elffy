#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Elffy.UI
{
    [DebuggerDisplay("{DebugDisplay}")]
    [StructLayout(LayoutKind.Explicit)]
    public struct LayoutThickness : IEquatable<LayoutThickness>
    {
        [FieldOffset(0)]
        public float Left;
        [FieldOffset(4)]
        public float Top;
        [FieldOffset(8)]
        public float Right;
        [FieldOffset(12)]
        public float Bottom;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebugDisplay => $"({Left}, {Top}, {Right}, {Bottom})";

        public LayoutThickness(float value)
        {
            Left = value;
            Top = value;
            Right = value;
            Bottom = value;
        }

        public LayoutThickness(float left, float top, float right, float bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public readonly override string ToString() => DebugDisplay;

        public readonly override bool Equals(object? obj) => obj is LayoutThickness thickness && Equals(thickness);

        public readonly bool Equals(LayoutThickness other) => Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;

        public readonly override int GetHashCode() => HashCode.Combine(Left, Top, Right, Bottom);

        public static bool operator ==(LayoutThickness left, LayoutThickness right) => left.Equals(right);

        public static bool operator !=(LayoutThickness left, LayoutThickness right) => !(left == right);
    }
}
