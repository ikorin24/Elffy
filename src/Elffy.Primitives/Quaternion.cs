﻿#nullable enable
using Elffy.Markup;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NQuat = System.Numerics.Quaternion;
using NVec3 = System.Numerics.Vector3;
using NVec4 = System.Numerics.Vector4;

namespace Elffy
{
    [DebuggerDisplay("{DebugView}")]
    [StructLayout(LayoutKind.Explicit)]
    [UseLiteralMarkup]
    [LiteralMarkupPattern(LiteralPattern, LiteralEmit)]
    public struct Quaternion : IEquatable<Quaternion>
    {
        private const string LiteralPattern = @$"^(?<x>{RegexPatterns.Float}), *(?<y>{RegexPatterns.Float}), *(?<z>{RegexPatterns.Float}), *(?<w>{RegexPatterns.Float})$";
        private const string LiteralEmit = @"new global::Elffy.Quaternion((float)(${x}), (float)(${y}), (float)(${z}), (float)(${w}))";

        [FieldOffset(0)]
        public float X;
        [FieldOffset(4)]
        public float Y;
        [FieldOffset(8)]
        public float Z;
        [FieldOffset(12)]
        public float W;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebugView => $"V:({X}, {Y}, {Z}), W:{W}";

        public static readonly Quaternion Identity = new Quaternion(0f, 0f, 0f, 1f);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Quaternion(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public Vector3 Xyz => new Vector3(X, Y, Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Normalize()
        {
            var len = Unsafe.As<Quaternion, NVec4>(ref this).Length();
            Unsafe.As<Quaternion, NVec4>(ref this) /= len;

            //var length = MathF.Sqrt((X * X) + (Y * Y) + (Z * Z) + (W * W));
            //X /= length;
            //Y /= length;
            //Z /= length;
            //W /= length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Quaternion Normalized()
        {
            var copy = this;
            copy.Normalize();
            return copy;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Invert() => (X, Y, Z) = (-X, -Y, -Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Quaternion Inverted() => new Quaternion(-X, -Y, -Z, W);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Matrix3 ToMatrix3()
        {
            var v = UnsafeAs<Quaternion, NVec4>(this) * UnsafeAs<Quaternion, NVec4>(this);
            var s = new NVec3(X) * new NVec3(Y, Z, W);
            var t = new NVec3(Y, Y, Z) * new NVec3(Z, W, W);

            Matrix3 result;
            result.M00 = v.X - v.Y - v.Z + v.W;

            result.M10 = s.X + t.Z;
            result.M20 = s.Y - t.Y;
            result.M01 = s.X - t.Z;
            Unsafe.As<float, NVec3>(ref result.M10) *= new NVec3(2f);

            result.M11 = -v.X + v.Y - v.Z + v.W;

            result.M21 = t.X + s.Z;
            result.M02 = s.Y + t.Y;
            result.M12 = t.X - s.Z;
            Unsafe.As<float, NVec3>(ref result.M21) *= new NVec3(2f);

            result.M22 = -v.X - v.Y + v.Z + v.W;
            return result;

            //var x2 = X * X;
            //var y2 = Y * Y;
            //var z2 = Z * Z;
            //var w2 = W * W;
            //var xy = X * Y;
            //var xz = X * Z;
            //var xw = X * W;
            //var yz = Y * Z;
            //var yw = Y * W;
            //var zw = Z * W;
            //return new Matrix3(x2 - y2 - z2 + w2,    2 * (xy - zw),       2 * (xz + yw),
            //                   2 * (xy + zw),        -x2 + y2 - z2 + w2,  2 * (yz - xw),
            //                   2 * (xz - yw),        2 * (yz + xw),       -x2 - y2 + z2 + w2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Matrix4 ToMatrix4() => new Matrix4(ToMatrix3());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FromAxisAngle(in Vector3 axis, float angle, out Quaternion result)
        {
            result = UnsafeAs<NQuat, Quaternion>(
                NQuat.CreateFromAxisAngle(UnsafeAs<Vector3, NVec3>(axis), angle)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion FromAxisAngle(in Vector3 axis, float angle)
        {
            FromAxisAngle(axis, angle, out var result);
            return result;
        }

        public static Quaternion FromTwoVectors(Vector3 from, Vector3 to)
        {
            var axis = Vector3.Cross(from, to).Normalized();
            if(axis.ContainsNaNOrInfinity) {
                return Identity;
            }
            var angle = Vector3.AngleBetween(from, to);
            if(float.IsNaN(angle)) {
                return Identity;
            }
            return FromAxisAngle(axis, angle);
        }

        public readonly override string ToString() => DebugView;

        public readonly override bool Equals(object? obj) => obj is Quaternion quaternion && Equals(quaternion);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Quaternion other) => (X == other.X) && (Y == other.Y) && (Z == other.Z) && (W == other.W);

        public readonly override int GetHashCode() => HashCode.Combine(X, Y, Z, W);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Quaternion left, in Quaternion right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Quaternion left, in Quaternion right) => !(left == right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion operator -(in Quaternion q) => q.Inverted();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion operator *(in Quaternion q, in Quaternion p)
        {
            //return new Quaternion( q.W * p.X - q.Z * p.Y + q.Y * p.Z + q.X * p.W,
            //                       q.Z * p.X + q.W * p.Y - q.X * p.Z + q.Y * p.W,
            //                      -q.Y * p.X + q.X * p.Y + q.W * p.Z + q.Z * p.W,
            //                      -q.X * p.X - q.Y * p.Y - q.Z * p.Z + q.W * p.W);

            var a = new NVec4(q.W, q.Z, -q.Y, -q.X) * p.X;
            var b = new NVec4(-q.Z, q.W, q.X, -q.Y) * p.Y;
            var c = new NVec4(q.Y, -q.X, q.W, -q.Z) * p.Z;
            var d = new NVec4(q.X, q.Y, q.Z, q.W) * p.W;
            var v = a + b + c + d;
            return Unsafe.As<NVec4, Quaternion>(ref v);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator *(in Quaternion rot, in Vector3 vec)
        {
            var a = new NVec4(rot.W, rot.Z, -rot.Y, -rot.X) * vec.X;
            var b = new NVec4(-rot.Z, rot.W, rot.X, -rot.Y) * vec.Y;
            var c = new NVec4(rot.Y, -rot.X, rot.W, -rot.Z) * vec.Z;
            var v = a + b + c;
            return (Unsafe.As<NVec4, Quaternion>(ref v) * rot.Inverted()).Xyz;

            //return (rot * new Quaternion(vec.X, vec.Y, vec.Z, 0) * rot.Inverted()).Xyz;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector4(in Quaternion quaternion) => Unsafe.As<Quaternion, Vector4>(ref Unsafe.AsRef(quaternion));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref readonly TTo UnsafeAs<TFrom, TTo>(in TFrom source)
        {
            return ref Unsafe.As<TFrom, TTo>(ref Unsafe.AsRef(source));
        }
    }
}
