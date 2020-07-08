#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Elffy.Mathematics;
using TKQuaternion = OpenToolkit.Mathematics.Quaternion;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion(in Vector3 axis, float radian)
        {
            var nAxis = axis.Normalized();
            var halfRadian = radian / 2;
            var sin = MathF.Sin(halfRadian);
            X = nAxis.X * sin;
            Y = nAxis.Y * sin;
            Z = nAxis.Z * sin;
            W = MathF.Cos(halfRadian);
        }

        public Vector3 Xyz => new Vector3(X, Y, Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Normalize()
        {
            var length = MathF.Sqrt((X * X) + (Y * Y) + (Z * Z) + (W * W));
            X /= length;
            Y /= length;
            Z /= length;
            W /= length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Quaternion Normalized()
        {
            var copy = new Quaternion(X, Y, Z, W);
            copy.Normalize();
            return copy;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Inverse() => (X, Y, Z) = (-X, -Y, -Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Quaternion Inversed() => new Quaternion(-X, -Y, -Z, W);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Matrix3 ToMatrix3()
        {
            var x2 = X * X;
            var y2 = Y * Y;
            var z2 = Z * Z;
            var w2 = W * W;

            var xy = X * Y;
            var xz = X * Z;
            var xw = X * W;
            var yz = Y * Z;
            var yw = Y * W;
            var zw = Z * W;

            return new Matrix3(x2 - y2 - z2 + w2,    2 * (xy - zw),       2 * (xz + yw),
                               2 * (xy + zw),        -x2 + y2 - z2 + w2,  2 * (yz - xw),
                               2 * (xz - yw),        2 * (yz + xw),       -x2 - y2 + z2 + w2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Matrix4 ToMatrix4() => new Matrix4(ToMatrix3());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override string ToString() => $"V:({X}, {Y}, {Z}), W:{W}";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override bool Equals(object? obj) => obj is Quaternion quaternion && Equals(quaternion);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Quaternion other) => (X == other.X) && (Y == other.Y) && (Z == other.Z) && (W == other.W);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Quaternion left, in Quaternion right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Quaternion left, in Quaternion right) => !(left == right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion operator *(in Quaternion q, in Quaternion p)
        {
            return new Quaternion( q.W * p.X - q.Z * p.Y + q.Y * p.Z + q.X * p.W,
                                   q.Z * p.X + q.W * p.Y - q.X * p.Z + q.Y * p.W,
                                  -q.Y * p.X + q.X * p.Y + q.W * p.Z + q.Z * p.W,
                                  -q.X * p.X - q.Y * p.Y - q.Z * p.Z + q.W * p.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator *(in Quaternion rot, in Vector3 vec)
        {
            return (rot * new Quaternion(vec.X, vec.Y, vec.Z, 0) * rot.Inversed()).Xyz;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector4(in Quaternion quaternion) => Unsafe.As<Quaternion, Vector4>(ref Unsafe.AsRef(quaternion));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator TKQuaternion(Quaternion q) => Unsafe.As<Quaternion, TKQuaternion>(ref q);
    }
}
