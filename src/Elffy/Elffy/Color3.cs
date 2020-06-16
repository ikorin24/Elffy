#nullable enable
using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TKColor4 = OpenToolkit.Mathematics.Color4;

namespace Elffy
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Color3 : IEquatable<Color3>
    {
        [FieldOffset(0)]
        public float R;
        [FieldOffset(4)]
        public float G;
        [FieldOffset(8)]
        public float B;

        public Color3(float r, float g, float b) => (R, G, B) = (r, g, b);
        public Color3(float value) => (R, G, B) = (value, value, value);

        public readonly override bool Equals(object? obj) => obj is Color3 color && Equals(color);

        public readonly bool Equals(Color3 other) => (R == other.R) && (G == other.G) && (B == other.B);

        public readonly override int GetHashCode() => HashCode.Combine(R, G, B);

        public readonly override string ToString() => $"(R, G, B) = ({R}, {G}, {B}) = ({ToByte(R)}, {ToByte(G)}, {ToByte(B)})";

        public static bool operator ==(in Color3 left, in Color3 right) => left.Equals(right);

        public static bool operator !=(in Color3 left, in Color3 right) => !(left == right);

        public static implicit operator TKColor4(Color3 color) => new TKColor4(color.R, color.G, color.B, 1f);
        public static implicit operator Color3(TKColor4 color) => Unsafe.As<TKColor4, Color3>(ref color);
        public static explicit operator Color(in Color3 color) => (Color)(TKColor4)color;
        public static implicit operator Color3(in Color color) => (TKColor4)color;
        public static explicit operator Vector3(Color3 color) => Unsafe.As<Color3, Vector3>(ref color);
        public static explicit operator Color4(Color3 color) => new Color4(color);
        public static explicit operator Color3(Color4 color) => Unsafe.As<Color4, Color3>(ref color);

        private static byte ToByte(float value)
        {
            var tmp = value * byte.MaxValue;
            return (tmp < 0f) ? (byte)0 :
                   (tmp > (float)byte.MaxValue) ? byte.MaxValue : (byte)tmp;
        }
    }
}
