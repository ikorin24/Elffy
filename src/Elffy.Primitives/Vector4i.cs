#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Elffy.Markup;
using N = System.Numerics;

namespace Elffy
{
    [StructLayout(LayoutKind.Explicit)]
    [DebuggerDisplay("{DebuggerDisplay}")]
    [UseLiteralMarkup]
    [LiteralMarkupPattern(LiteralPattern, LiteralEmit)]
    public struct Vector4i : IEquatable<Vector4i>
    {
        private const string LiteralPattern = @$"^(?<x>{RegexPatterns.Int}), *(?<y>{RegexPatterns.Int}), *(?<z>{RegexPatterns.Int}), *(?<w>{RegexPatterns.Int})$";
        private const string LiteralEmit = @"new global::Elffy.Vector4i((int)(${x}), (int)(${y}), (int)(${z}), (int)(${w}))";

        [FieldOffset(0)]
        public int X;
        [FieldOffset(4)]
        public int Y;
        [FieldOffset(8)]
        public int Z;
        [FieldOffset(12)]
        public int W;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebuggerDisplay => $"({X}, {Y}, {Z}, {W})";

        public static Vector4i UnitX => new Vector4i(1, 0, 0, 0);
        public static Vector4i UnitY => new Vector4i(0, 1, 0, 0);
        public static Vector4i UnitZ => new Vector4i(0, 0, 1, 0);
        public static Vector4i UnitW => new Vector4i(0, 0, 0, 1);
        public static Vector4i Zero => new Vector4i(0, 0, 0, 0);
        public static Vector4i One => new Vector4i(1, 1, 1, 1);
        public static unsafe int SizeInBytes => sizeof(Vector4i);

        public readonly bool IsZero => this == default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4i(int x, int y, int z, int w) => (X, Y, Z, W) = (x, y, z, w);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4i(int value)
        {
            X = value;
            Y = value;
            Z = value;
            W = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Destructor(out int x, out int y, out int z, out int w) => (x, y, z, w) = (X, Y, Z, W);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int SumElement() => X + Y + Z + W;

        public readonly override bool Equals(object? obj) => obj is Vector4i i && Equals(i);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Vector4i other)
        {
            ref var v1 = ref Unsafe.As<Vector4i, N.Vector<int>>(ref Unsafe.AsRef(in this));
            ref var v2 = ref Unsafe.As<Vector4i, N.Vector<int>>(ref Unsafe.AsRef(in other));
            return N.Vector.EqualsAll(v1, v2);
        }

        public readonly override int GetHashCode() => HashCode.Combine(X, Y, Z, W);

        public readonly override string ToString() => DebuggerDisplay;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4i operator -(in Vector4i vec) => new Vector4i(-vec.X, -vec.Y, -vec.Z, -vec.W);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Vector4i left, in Vector4i right) => left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Vector4i left, in Vector4i right) => !(left == right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4i operator +(in Vector4i vec1, in Vector4i vec2) => new Vector4i(vec1.X + vec2.X, vec1.Y + vec2.Y, vec1.Z + vec2.Z, vec1.W + vec2.W);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4i operator +(in Vector4i vec, int right) => new Vector4i(vec.X + right, vec.Y + right, vec.Z + right, vec.W + right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4i operator +(int left, in Vector4i vec) => new Vector4i(left + vec.X, left + vec.Y, left + vec.Z, left + vec.W);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4i operator -(in Vector4i vec1, in Vector4i vec2) => new Vector4i(vec1.X - vec2.X, vec1.Y - vec2.Y, vec1.Z - vec2.Z, vec1.W - vec2.W);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4i operator -(in Vector4i vec, int right) => new Vector4i(vec.X - right, vec.Y - right, vec.Z - right, vec.W - right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4i operator -(int left, in Vector4i vec) => new Vector4i(left - vec.X, left - vec.Y, left - vec.Z, left - vec.W);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4i operator *(in Vector4i vec1, in Vector4i vec2) => new Vector4i(vec1.X * vec2.X, vec1.Y * vec2.Y, vec1.Z * vec2.Z, vec1.W * vec2.W);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4i operator *(in Vector4i vec, int right) => new Vector4i(vec.X * right, vec.Y * right, vec.Z * right, vec.W * right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4i operator *(int left, in Vector4i vec) => new Vector4i(left * vec.X, left * vec.Y, left * vec.Z, left * vec.W);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4i operator /(in Vector4i vec1, in Vector4i vec2) => new Vector4i(vec1.X / vec2.X, vec1.Y / vec2.Y, vec1.Z / vec2.Z, vec1.W / vec2.W);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4i operator /(in Vector4i vec, int right) => new Vector4i(vec.X / right, vec.Y / right, vec.Z / right, vec.W / right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4i operator /(int left, in Vector4i vec) => new Vector4i(left / vec.X, left / vec.Y, left / vec.Z, left / vec.W);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector4i(in Vector4 vec) => new Vector4i((int)vec.X, (int)vec.Y, (int)vec.Z, (int)vec.W);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector4(in Vector4i vec) => new Vector4(vec.X, vec.Y, vec.Z, vec.W);
    }
}
