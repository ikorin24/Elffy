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
    public struct Vector2u : IEquatable<Vector2u>
    {
        [FieldOffset(0)]
        public uint X;
        [FieldOffset(4)]
        public uint Y;

        public ref uint this[int index]
        {
            [UnscopedRef]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if((uint)index >= 2) { throw new IndexOutOfRangeException(nameof(index)); }
                return ref Unsafe.Add(ref X, index);
            }
        }

        public static Vector2u UnitX => new Vector2u(1, 0);
        public static Vector2u UnitY => new Vector2u(0, 1);
        public static Vector2u Zero => new Vector2u(0, 0);
        public static Vector2u One => new Vector2u(1, 1);
        public static unsafe int SizeInBytes => sizeof(Vector2u);

        public readonly bool IsZero => this == default;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebuggerDisplay => $"({X}, {Y})";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2u(uint x, uint y) => (X, Y) = (x, y);

        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out uint x, out uint y) => (x, y) = (X, Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly uint SumElements() => X + Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector2 ToVector2() => new Vector2((float)X, (float)Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector2i ToVector2i() => new Vector2i((int)X, (int)Y);

        public override bool Equals(object? obj) => obj is Vector2u i && Equals(i);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector2u other) => X == other.X && Y == other.Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => HashCode.Combine(X, Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override string ToString() => DebuggerDisplay;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Vector2u left, in Vector2u right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Vector2u left, in Vector2u right) => !(left == right);

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static Vector2u operator -(in Vector2u vec) => new Vector2l(-vec.X, -vec.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2u operator +(in Vector2u vec1, in Vector2u vec2) => new Vector2u(vec1.X + vec2.X, vec1.Y + vec2.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2u operator +(in Vector2u vec, uint right) => new Vector2u(vec.X + right, vec.Y + right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2u operator +(uint left, in Vector2u vec) => new Vector2u(left + vec.X, left + vec.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2u operator -(in Vector2u vec1, in Vector2u vec2) => new Vector2u(vec1.X - vec2.X, vec1.Y - vec2.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2u operator -(in Vector2u vec, uint right) => new Vector2u(vec.X - right, vec.Y - right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2u operator -(uint left, in Vector2u vec) => new Vector2u(left - vec.X, left - vec.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2u operator *(in Vector2u vec1, in Vector2u vec2) => new Vector2u(vec1.X * vec2.X, vec1.Y * vec2.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2u operator *(in Vector2u vec, uint right) => new Vector2u(vec.X * right, vec.Y * right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2u operator *(uint left, in Vector2u vec) => new Vector2u(left * vec.X, left * vec.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2u operator /(in Vector2u vec1, in Vector2u vec2) => new Vector2u(vec1.X / vec2.X, vec1.Y / vec2.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2u operator /(in Vector2u vec, uint right) => new Vector2u(vec.X / right, vec.Y / right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2u operator /(uint left, in Vector2u vec) => new Vector2u(left / vec.X, left / vec.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector2u(in Vector2 vec) => new Vector2u((uint)vec.X, (uint)vec.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector2(in Vector2u vec) => new Vector2(vec.X, vec.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector2u(in Vector2i vec) => new Vector2u((uint)vec.X, (uint)vec.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector2i(in Vector2u vec) => new Vector2i((int)vec.X, (int)vec.Y);
    }
}
