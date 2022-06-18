#nullable enable
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Elffy.Markup;

namespace Elffy
{
    [StructLayout(LayoutKind.Explicit)]
    [DebuggerDisplay("{DebuggerDisplay}")]
    [UseLiteralMarkup]
    [LiteralMarkupPattern(LiteralPattern, LiteralEmit)]
    public struct Vector2i : IEquatable<Vector2i>
    {
        private const string LiteralPattern = @$"^(?<x>{RegexPatterns.Int}), *(?<y>{RegexPatterns.Int})$";
        private const string LiteralEmit = @"new global::Elffy.Vector2i((int)(${x}), (int)(${y}))";

        [FieldOffset(0)]
        public int X;
        [FieldOffset(4)]
        public int Y;

        public static Vector2i UnitX => new Vector2i(1, 0);
        public static Vector2i UnitY => new Vector2i(0, 1);
        public static Vector2i Zero => new Vector2i(0, 0);
        public static Vector2i One => new Vector2i(1, 1);
        public static unsafe int SizeInBytes => sizeof(Vector2i);

        public readonly bool IsZero => this == default;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebuggerDisplay => $"({X}, {Y})";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2i(int x, int y) => (X, Y) = (x, y);

        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out int x, out int y) => (x, y) = (X, Y);

        public override bool Equals(object? obj) => obj is Vector2i i && Equals(i);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector2i other) => X == other.X && Y == other.Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => HashCode.Combine(X, Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override string ToString() => DebuggerDisplay;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Vector2i left, in Vector2i right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Vector2i left, in Vector2i right) => !(left == right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2i operator -(in Vector2i vec) => new Vector2i(-vec.X, -vec.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2i operator +(in Vector2i vec1, in Vector2i vec2) => new Vector2i(vec1.X + vec2.X, vec1.Y + vec2.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2i operator +(in Vector2i vec, int right) => new Vector2i(vec.X + right, vec.Y + right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2i operator +(int left, in Vector2i vec) => new Vector2i(left + vec.X, left + vec.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2i operator -(in Vector2i vec1, in Vector2i vec2) => new Vector2i(vec1.X - vec2.X, vec1.Y - vec2.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2i operator -(in Vector2i vec, int right) => new Vector2i(vec.X - right, vec.Y - right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2i operator -(int left, in Vector2i vec) => new Vector2i(left - vec.X, left - vec.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2i operator *(in Vector2i vec1, in Vector2i vec2) => new Vector2i(vec1.X * vec2.X, vec1.Y * vec2.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2i operator *(in Vector2i vec, int right) => new Vector2i(vec.X * right, vec.Y * right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2i operator *(int left, in Vector2i vec) => new Vector2i(left * vec.X, left * vec.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2i operator /(in Vector2i vec1, in Vector2i vec2) => new Vector2i(vec1.X / vec2.X, vec1.Y / vec2.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2i operator /(in Vector2i vec, int right) => new Vector2i(vec.X / right, vec.Y / right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator /(int left, in Vector2i vec) => new Vector2(left / vec.X, left / vec.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector2i(in Vector2 vec) => new Vector2i((int)vec.X, (int)vec.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector2(in Vector2i vec) => new Vector2(vec.X, vec.Y);
    }
}
