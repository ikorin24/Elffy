#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Elffy.Mathmatics;
using TKQuaternion = OpenTK.Quaternion;

namespace Elffy
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Quaternion : IEquatable<Quaternion>
    {
        [FieldOffset(0)]
        public float X;
        [FieldOffset(4)]
        public float Y;
        [FieldOffset(8)]
        public float Z;
        [FieldOffset(12)]
        public float W;

        public static readonly Quaternion Identity = new Quaternion(0f, 0f, 0f, 1f);

        public Quaternion(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public Quaternion(in Vector3 axis, float angle)
        {
            var nAxis = axis.Normalized();
            var halfAngle = angle / 2;
            var sin = MathTool.Sin(halfAngle);
            X = nAxis.X * sin;
            Y = nAxis.Y * sin;
            Z = nAxis.Z * sin;
            W = MathTool.Cos(halfAngle);
        }

        public Vector3 Xyz => new Vector3(X, Y, Z);

        public void Normalize()
        {
            var length = (float)Math.Sqrt((X * X) + (Y * Y) + (Z * Z) + (W * W));
            X /= length;
            Y /= length;
            Z /= length;
            W /= length;
        }

        public readonly Quaternion Normalized()
        {
            var copy = new Quaternion(X, Y, Z, W);
            copy.Normalize();
            return copy;
        }

        public void Inverse() => (X, Y, Z) = (-X, -Y, -Z);

        public readonly Quaternion Inversed() => new Quaternion(-X, -Y, -Z, W);

        public readonly override string ToString() => $"V:({X}, {Y}, {Z}), W:{W}";

        public readonly override bool Equals(object? obj) => obj is Quaternion quaternion && Equals(quaternion);

        public readonly bool Equals(Quaternion other) => (X == other.X) && (Y == other.Y) && (Z == other.Z) && (W == other.W);

        public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);

        public static bool operator ==(in Quaternion left, in Quaternion right) => left.Equals(right);

        public static bool operator !=(in Quaternion left, in Quaternion right) => !(left == right);

        public static Quaternion operator *(in Quaternion q, in Quaternion p)
        {
            return new Quaternion( q.W * p.X - q.Z * p.Y + q.Y * p.Z + q.X * p.W,
                                   q.Z * p.X + q.W * p.Y - q.X * p.Z + q.Y * p.W,
                                  -q.Y * p.X + q.X * p.Y + q.W * p.Z + q.Z * p.W,
                                  -q.X * p.X - q.Y * p.Y - q.Z * p.Z + q.W * p.W);
        }

        public static explicit operator Vector4(in Quaternion quaternion) => Unsafe.As<Quaternion, Vector4>(ref Unsafe.AsRef(quaternion));
    }
}
