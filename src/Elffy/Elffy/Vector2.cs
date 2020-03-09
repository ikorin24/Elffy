#nullable enable
using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TKVector2 = OpenTK.Vector2;

namespace Elffy
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct Vector2 : IEquatable<Vector2>
    {
        [FieldOffset(0)]
        public readonly float X;
        [FieldOffset(4)]
        public readonly float Y;

        public static readonly Vector2 UnitX = new Vector2(1, 0);
        public static readonly Vector2 UnitY = new Vector2(0, 1);
        public static readonly Vector2 Zero = new Vector2(0, 0);
        public static readonly Vector2 One = new Vector2(1, 1);
        public static unsafe readonly int SizeInBytes = sizeof(Vector2);

        public readonly Vector2 Yx => new Vector2(Y, X);

        public readonly float LengthSquared => (X * X) + (Y * Y);
        public readonly float Length => (float)Math.Sqrt(LengthSquared);

        public Vector2(float x, float y) => (X, Y) = (x, y);
        public Vector2(float value) => (X, Y) = (value, value);

        public readonly (float X, float Y) ToTuple() => (X, Y);
        public readonly float SumElement() => X + Y;
        public readonly float Dot(in Vector2 vec) => Mult(this, vec).SumElement();
        public static float Dot(in Vector2 vec1, in Vector2 vec2) => vec1.Dot(vec2);
        public readonly Vector2 Mult(in Vector2 vec) => new Vector2(X * vec.X, Y * vec.Y);
        public static Vector2 Mult(in Vector2 vec1, in Vector2 vec2) => vec1.Mult(vec2);
        public readonly Vector2 Normalized() => ((TKVector2)this).Normalized();
        public readonly override bool Equals(object? obj) => obj is Vector2 vector && Equals(vector);
        public readonly bool Equals(Vector2 other) => X == other.X && Y == other.Y;
        public readonly override int GetHashCode() => HashCode.Combine(X, Y);
        public readonly override string ToString() => $"({X}, {Y})";

        public static Vector2 operator -(in Vector2 vec) => new Vector2(-vec.X, -vec.Y);
        public static bool operator ==(in Vector2 left, in Vector2 right) => left.Equals(right);
        public static bool operator !=(in Vector2 left, in Vector2 right) => !(left == right);
        public static Vector2 operator +(in Vector2 vec1, in Vector2 vec2) => new Vector2(vec1.X + vec2.X, vec1.Y + vec2.Y);
        public static Vector2 operator +(in Vector2 vec1, float right) => new Vector2(vec1.X + right, vec1.Y + right);
        public static Vector2 operator -(in Vector2 vec1, in Vector2 vec2) => new Vector2(vec1.X - vec2.X, vec1.Y - vec2.Y);
        public static Vector2 operator -(in Vector2 vec1, float right) => new Vector2(vec1.X - right, vec1.Y - right);
        public static Vector2 operator *(in Vector2 vec1, float right) => new Vector2(vec1.X * right, vec1.Y * right);
        public static Vector2 operator *(float right, in Vector2 vec1) => new Vector2(vec1.X * right, vec1.Y * right);
        public static Vector2 operator /(in Vector2 vec1, float right) => new Vector2(vec1.X / right, vec1.Y / right);
        public static Vector2 operator /(float right, in Vector2 vec1) => new Vector2(vec1.X / right, vec1.Y / right);
        public static implicit operator TKVector2(Vector2 vec) => Unsafe.As<Vector2, TKVector2>(ref vec);
        public static implicit operator Vector2(TKVector2 vec) => Unsafe.As<TKVector2, Vector2>(ref vec);
        public static implicit operator Vector2(in Point point) => new Vector2(point.X, point.Y);
        public static explicit operator Point(in Vector2 vec) => new Point((int)vec.X, (int)vec.Y);
        public static implicit operator PointF(Vector2 vec) => Unsafe.As<Vector2, PointF>(ref vec);
        public static implicit operator Vector2(PointF point) => Unsafe.As<PointF, Vector2>(ref point);
    }
}
