#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TKVector3 = OpenTK.Vector3;

namespace Elffy
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Vector3 : IEquatable<Vector3>
    {
        [FieldOffset(0)]
        public float X;
        [FieldOffset(4)]
        public float Y;
        [FieldOffset(8)]
        public float Z;

        public static readonly Vector3 UnitX = new Vector3(1, 0, 0);
        public static readonly Vector3 UnitY = new Vector3(0, 1, 0);
        public static readonly Vector3 UnitZ = new Vector3(0, 0, 1);
        public static readonly Vector3 Zero = new Vector3(0, 0, 0);
        public static readonly Vector3 One = new Vector3(1, 1, 1);
        public static unsafe readonly int SizeInBytes = sizeof(Vector3);

        public readonly Vector2 Xy => new Vector2(X, Y);
        public readonly Vector2 Xz => new Vector2(X, Z);
        public readonly Vector2 Yx => new Vector2(Y, X);
        public readonly Vector2 Yz => new Vector2(Y, Z);
        public readonly Vector2 Zx => new Vector2(Z, X);
        public readonly Vector2 Zy => new Vector2(Z, Y);
        public readonly Vector3 Xzy => new Vector3(X, Z, Y);
        public readonly Vector3 Yxz => new Vector3(Y, X, Z);
        public readonly Vector3 Yzx => new Vector3(Y, Z, X);
        public readonly Vector3 Zxy => new Vector3(Z, X, Y);
        public readonly Vector3 Zyx => new Vector3(Z, Y, X);

        public readonly float LengthSquared => (X * X) + (Y * Y) + (Z * Z);
        public readonly float Length => (float)Math.Sqrt(LengthSquared);

        public Vector3(float x, float y, float z) => (X, Y, Z) = (x, y, z);
        public Vector3(Vector3 v) => this = v;
        public Vector3(Vector2 v) => (X, Y, Z) = (v.X, v.Y, 0);
        public Vector3(Vector2 v, float z) => (X, Y, Z) = (v.X, v.Y, z);
        public Vector3(in Vector4 v) => (X, Y, Z) = (v.X, v.Y, v.Z);
        public Vector3(float value) => (X, Y, Z) = (value, value, value);

        public readonly (float X, float Y, float Z) ToTuple() => (X, Y, Z);
        public readonly Vector3 Dot(in Vector3 vec) => this * vec;
        public static Vector3 Dot(in Vector3 vec1, in Vector3 vec2) => vec1 * vec2;
        public readonly Vector3 Cross(in Vector3 vec) => Cross(this, vec);
        public static Vector3 Cross(in Vector3 vec1, in Vector3 vec2) => new Vector3(vec1.Y * vec2.Z - vec1.Z * vec2.Y,
                                                                               vec1.Z * vec2.X - vec1.X * vec2.Z,
                                                                               vec1.X * vec2.Y - vec1.Y * vec2.X);
        public readonly Vector3 Normalized() => ((TKVector3)this).Normalized();
        public readonly override bool Equals(object? obj) => obj is Vector3 vector && Equals(vector);
        public readonly bool Equals(Vector3 other) => X == other.X && Y == other.Y && Z == other.Z;
        public readonly override int GetHashCode() => HashCode.Combine(X, Y, Z);
        public readonly override string ToString() => $"({X}, {Y}, {Z})";

        public static Vector3 operator -(in Vector3 vec) => new Vector3(-vec.X, -vec.Y, -vec.Z);
        public static bool operator ==(in Vector3 left, in Vector3 right) => left.Equals(right);
        public static bool operator !=(in Vector3 left, in Vector3 right) => !(left == right);
        public static Vector3 operator +(in Vector3 vec1, in Vector3 vec2) => new Vector3(vec1.X + vec2.X, vec1.Y + vec2.Y, vec1.Z + vec2.Z);
        public static Vector3 operator +(in Vector3 vec1, float right) => new Vector3(vec1.X + right, vec1.Y + right, vec1.Z + right);
        public static Vector3 operator -(in Vector3 vec1, in Vector3 vec2) => new Vector3(vec1.X - vec2.X, vec1.Y - vec2.Y, vec1.Z - vec2.Z);
        public static Vector3 operator -(in Vector3 vec1, float right) => new Vector3(vec1.X - right, vec1.Y - right, vec1.Z - right);
        public static Vector3 operator *(in Vector3 vec1, in Vector3 vec2) => new Vector3(vec1.X * vec2.X, vec1.Y * vec2.Y, vec1.Z * vec2.Z);
        public static Vector3 operator *(in Vector3 vec1, float right) => new Vector3(vec1.X * right, vec1.Y * right, vec1.Z * right);
        public static Vector3 operator *(float right, in Vector3 vec1) => new Vector3(vec1.X * right, vec1.Y * right, vec1.Z * right);
        public static Vector3 operator /(in Vector3 vec1, float right) => new Vector3(vec1.X / right, vec1.Y / right, vec1.Z / right);
        public static Vector3 operator /(float right, in Vector3 vec1) => new Vector3(vec1.X / right, vec1.Y / right, vec1.Z / right);

        public static implicit operator TKVector3(Vector3 vec) => Unsafe.As<Vector3, TKVector3>(ref vec);
        public static implicit operator Vector3(TKVector3 vec) => Unsafe.As<TKVector3, Vector3>(ref vec);
    }
}
