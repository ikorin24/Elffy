#nullable enable
using System;
using System.Runtime.InteropServices;

namespace Elffy
{
    [StructLayout(LayoutKind.Explicit)]
    public struct ColorByte : IEquatable<ColorByte>
    {
        [FieldOffset(0)]
        public byte R;
        [FieldOffset(1)]
        public byte G;
        [FieldOffset(2)]
        public byte B;
        [FieldOffset(3)]
        public byte A;

        public ColorByte(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public readonly void Deconstruct(out byte r, out byte g, out byte b, out byte a) => (r, g, b, a) = (R, G, B, A);

        public readonly override bool Equals(object? obj) => obj is ColorByte color && Equals(color);

        public readonly bool Equals(ColorByte other) => R == other.R && G == other.G && B == other.B && A == other.A;

        public readonly override int GetHashCode() => HashCode.Combine(R, G, B, A);

        public static bool operator ==(in ColorByte left, in ColorByte right) => left.Equals(right);

        public static bool operator !=(in ColorByte left, in ColorByte right) => !(left == right);
    }
}
