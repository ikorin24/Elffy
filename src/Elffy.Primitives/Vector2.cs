#nullable enable
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Elffy
{
    [StructLayout(LayoutKind.Explicit)]
    [DebuggerDisplay("{DebuggerDisplay}")]
    public struct Vector2 : IEquatable<Vector2>
    {
        [FieldOffset(0)]
        public float X;
        [FieldOffset(4)]
        public float Y;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebuggerDisplay => $"({X}, {Y})";

        public static Vector2 UnitX => new Vector2(1, 0);
        public static Vector2 UnitY => new Vector2(0, 1);
        public static Vector2 Zero => new Vector2(0, 0);
        public static Vector2 One => new Vector2(1, 1);
        public static unsafe int SizeInBytes => sizeof(Vector2);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector2 Yx => new Vector2(Y, X);

        public readonly float LengthSquared
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (X * X) + (Y * Y);
        }
        public readonly float Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MathF.Sqrt(LengthSquared);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2(float x, float y) => (X, Y) = (x, y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2(float value) => (X, Y) = (value, value);

        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out float x, out float y) => (x, y) = (X, Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float SumElement() => X + Y;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float Dot(in Vector2 vec) => Mult(this, vec).SumElement();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(in Vector2 vec1, in Vector2 vec2) => vec1.Dot(vec2);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector2 Mult(in Vector2 vec) => new Vector2(X * vec.X, Y * vec.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Mult(in Vector2 vec1, in Vector2 vec2) => vec1.Mult(vec2);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector2 Normalized()
        {
            var len = Length;
            return new Vector2(X / len, Y / len);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override bool Equals(object? obj) => obj is Vector2 vector && Equals(vector);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Vector2 other) => X == other.X && Y == other.Y;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override int GetHashCode() => HashCode.Combine(X, Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override string ToString() => DebuggerDisplay;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator -(in Vector2 vec) => new Vector2(-vec.X, -vec.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Vector2 left, in Vector2 right) => left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Vector2 left, in Vector2 right) => !(left == right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator +(in Vector2 vec1, in Vector2 vec2) => new Vector2(vec1.X + vec2.X, vec1.Y + vec2.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator +(in Vector2 vec1, float right) => new Vector2(vec1.X + right, vec1.Y + right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator -(in Vector2 vec1, in Vector2 vec2) => new Vector2(vec1.X - vec2.X, vec1.Y - vec2.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator -(in Vector2 vec1, float right) => new Vector2(vec1.X - right, vec1.Y - right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator *(in Vector2 vec1, float right) => new Vector2(vec1.X * right, vec1.Y * right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator *(float right, in Vector2 vec1) => new Vector2(vec1.X * right, vec1.Y * right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator /(in Vector2 vec1, float right) => new Vector2(vec1.X / right, vec1.Y / right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator /(in Vector2 vec1, in Vector2 vec2) => new Vector2(vec1.X / vec2.X, vec1.Y / vec2.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator /(in Vector2 vec1, in Vector2i vec2) => new Vector2(vec1.X / vec2.X, vec1.Y / vec2.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator /(float right, in Vector2 vec1) => new Vector2(vec1.X / right, vec1.Y / right);
    }
}
