#nullable enable
using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Elffy
{
    /// <summary>Color structure with RGBA bytes format</summary>
    [DebuggerDisplay("{DebugView,nq}")]
    [StructLayout(LayoutKind.Explicit)]
    public partial struct ColorByte : IEquatable<ColorByte>
    {
        [FieldOffset(0)]
        public byte R;
        [FieldOffset(1)]
        public byte G;
        [FieldOffset(2)]
        public byte B;
        [FieldOffset(3)]
        public byte A;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebugView => $"(R, G, B, A) = ({R}, {G}, {B}, {A})";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ColorByte(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out byte r, out byte g, out byte b, out byte a) => (r, g, b, a) = (R, G, B, A);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Color3 ToColor3() => new(R / byte.MaxValue, G / byte.MaxValue, B / byte.MaxValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Color4 ToColor4() => new(R / byte.MaxValue, G / byte.MaxValue, B / byte.MaxValue, A / byte.MaxValue);

        public readonly override bool Equals(object? obj) => obj is ColorByte color && Equals(color);

        public readonly bool Equals(ColorByte other) => R == other.R && G == other.G && B == other.B && A == other.A;

        public readonly override int GetHashCode() => HashCode.Combine(R, G, B, A);

        public static bool operator ==(in ColorByte left, in ColorByte right) => left.Equals(right);

        public static bool operator !=(in ColorByte left, in ColorByte right) => !(left == right);

        public readonly override string ToString() => DebugView;
    }
}
