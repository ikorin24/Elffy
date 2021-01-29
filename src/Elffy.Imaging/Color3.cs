#nullable enable
using Elffy.Effective.Unsafes;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy
{
    [DebuggerDisplay("{DebugView}")]
    [StructLayout(LayoutKind.Explicit)]
    public struct Color3 : IEquatable<Color3>
    {
        [FieldOffset(0)]
        public float R;
        [FieldOffset(4)]
        public float G;
        [FieldOffset(8)]
        public float B;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebugView => $"(R, G, B) = ({ToByte(R)}, {ToByte(G)}, {ToByte(B)}) = ({R}, {G}, {B})";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color3(float r, float g, float b) => (R, G, B) = (r, g, b);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color3(float value) => (R, G, B) = (value, value, value);

        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out float r, out float g, out float b) => (r, g, b) = (R, G, B);

        public readonly override bool Equals(object? obj) => obj is Color3 color && Equals(color);

        public readonly bool Equals(Color3 other) => (R == other.R) && (G == other.G) && (B == other.B);

        public readonly override int GetHashCode() => HashCode.Combine(R, G, B);

        public readonly override string ToString() => DebugView;

        public static bool operator ==(in Color3 left, in Color3 right) => left.Equals(right);

        public static bool operator !=(in Color3 left, in Color3 right) => !(left == right);

        public static explicit operator Color(in Color3 color) => Color.FromArgb(ToByte(color.R), ToByte(color.G), ToByte(color.B));
        public static explicit operator Color3(in Color color) => new Color3(color.R / byte.MaxValue, color.G / byte.MaxValue, color.B / byte.MaxValue);
        public static explicit operator Color4(in Color3 color) => new Color4(color);
        public static explicit operator Color3(in Color4 color) => UnsafeEx.As<Color4, Color3>(in color);

        private static byte ToByte(float value)
        {
            var tmp = value * byte.MaxValue;
            return (tmp < 0f) ? (byte)0 :
                   (tmp > (float)byte.MaxValue) ? byte.MaxValue : (byte)tmp;
        }
    }
}
