#nullable enable
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Elffy.Markup;
using System.Diagnostics.CodeAnalysis;

namespace Elffy
{
    [StructLayout(LayoutKind.Explicit)]
    [DebuggerDisplay("{DebuggerDisplay}")]
    [UseLiteralMarkup]
    [LiteralMarkupPattern(LiteralPattern, LiteralEmit)]
    public struct Vector3i : IEquatable<Vector3i>
    {
        private const string LiteralPattern = @$"^(?<x>{RegexPatterns.Int}), *(?<y>{RegexPatterns.Int}), *(?<z>{RegexPatterns.Int})$";
        private const string LiteralEmit = @"new global::Elffy.Vector3i((int)(${x}), (int)(${y}), (int)(${z}))";

        [FieldOffset(0)]
        public int X;
        [FieldOffset(4)]
        public int Y;
        [FieldOffset(8)]
        public int Z;

        public ref int this[int index]
        {
            [UnscopedRef]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if((uint)index >= 3) { throw new IndexOutOfRangeException(nameof(index)); }
                return ref Unsafe.Add(ref X, index);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebuggerDisplay => $"({X}, {Y}, {Z})";

        public static Vector3i UnitX => new Vector3i(1, 0, 0);
        public static Vector3i UnitY => new Vector3i(0, 1, 0);
        public static Vector3i UnitZ => new Vector3i(0, 0, 1);
        public static Vector3i Zero => new Vector3i(0, 0, 0);
        public static Vector3i One => new Vector3i(1, 1, 1);
        public static unsafe int SizeInBytes => sizeof(Vector3i);

        public readonly bool IsZero => this == default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3i(int x, int y, int z) => (X, Y, Z) = (x, y, z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3i(int value)
        {
            X = value;
            Y = value;
            Z = value;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out int x, out int y, out int z) => (x, y, z) = (X, Y, Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int SumElement() => X + Y + Z;

        public readonly override bool Equals(object? obj) => obj is Vector3i i && Equals(i);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Vector3i other) => X == other.X && Y == other.Y && Z == other.Z;

        public readonly override int GetHashCode() => HashCode.Combine(X, Y, Z);

        public readonly override string ToString() => DebuggerDisplay;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator -(in Vector3i vec) => new Vector3i(-vec.X, -vec.Y, -vec.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Vector3i left, in Vector3i right) => left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Vector3i left, in Vector3i right) => !(left == right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator +(in Vector3i vec1, in Vector3i vec2) => new Vector3i(vec1.X + vec2.X, vec1.Y + vec2.Y, vec1.Z + vec2.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator +(in Vector3i vec, int right) => new Vector3i(vec.X + right, vec.Y + right, vec.Z + right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator +(int left, in Vector3i vec) => new Vector3i(left + vec.X, left + vec.Y, left + vec.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator -(in Vector3i vec1, in Vector3i vec2) => new Vector3i(vec1.X - vec2.X, vec1.Y - vec2.Y, vec1.Z - vec2.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator -(in Vector3i vec1, int right) => new Vector3i(vec1.X - right, vec1.Y - right, vec1.Z - right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator -(int left, in Vector3i vec) => new Vector3i(left - vec.X, left - vec.Y, left - vec.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator *(in Vector3i vec1, in Vector3i vec2) => new Vector3i(vec1.X * vec2.X, vec1.Y * vec2.Y, vec1.Z * vec2.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator *(in Vector3i vec, int right) => new Vector3i(vec.X * right, vec.Y * right, vec.Z * right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator *(int left, in Vector3i vec) => new Vector3i(left * vec.X, left * vec.Y, left * vec.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator /(in Vector3i vec1, in Vector3i vec2) => new Vector3i(vec1.X / vec2.X, vec1.Y / vec2.Y, vec1.Z / vec2.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator /(in Vector3i vec1, int right) => new Vector3i(vec1.X / right, vec1.Y / right, vec1.Z / right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator /(int left, in Vector3i vec1) => new Vector3i(left / vec1.X, left / vec1.Y, left / vec1.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector3i(in Vector3 vec) => new Vector3i((int)vec.X, (int)vec.Y, (int)vec.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector3(in Vector3i vec) => new Vector3(vec.X, vec.Y, vec.Z);
    }
}
