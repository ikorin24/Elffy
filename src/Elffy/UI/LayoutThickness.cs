#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Cysharp.Text;

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
        private string DebugDisplay
        {
            get
            {
                using var sb = ZString.CreateStringBuilder(false);
                sb.AppendFormat("({0}, {1}, {2}, {3})", Left, Top, Right, Bottom);
                return sb.ToString();
            }
        }

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

        public override string ToString() => DebugDisplay;

        public override bool Equals(object? obj) => obj is LayoutThickness thickness && Equals(thickness);

        public bool Equals(LayoutThickness other) => Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;

        public override int GetHashCode() => HashCode.Combine(Left, Top, Right, Bottom);

        public static bool operator ==(LayoutThickness left, LayoutThickness right) => left.Equals(right);

        public static bool operator !=(LayoutThickness left, LayoutThickness right) => !(left == right);
    }
}
