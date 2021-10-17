#nullable enable
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using NVec3 = System.Numerics.Vector3;

namespace Elffy
{
    [StructLayout(LayoutKind.Explicit)]
    [DebuggerDisplay("{DebuggerDisplay}")]
    public struct Vector3 : IEquatable<Vector3>
    {
        [FieldOffset(0)]
        public float X;
        [FieldOffset(4)]
        public float Y;
        [FieldOffset(8)]
        public float Z;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebuggerDisplay => $"({X}, {Y}, {Z})";

        public static Vector3 UnitX => new Vector3(1, 0, 0);
        public static Vector3 UnitY => new Vector3(0, 1, 0);
        public static Vector3 UnitZ => new Vector3(0, 0, 1);
        public static Vector3 Zero => new Vector3(0, 0, 0);
        public static Vector3 One => new Vector3(1, 1, 1);
        public static unsafe int SizeInBytes => sizeof(Vector3);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector2 Xy => new Vector2(X, Y);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector2 Xz => new Vector2(X, Z);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector2 Yx => new Vector2(Y, X);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector2 Yz => new Vector2(Y, Z);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector2 Zx => new Vector2(Z, X);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector2 Zy => new Vector2(Z, Y);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Xzy => new Vector3(X, Z, Y);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Yxz => new Vector3(Y, X, Z);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Yzx => new Vector3(Y, Z, X);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Zxy => new Vector3(Z, X, Y);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Zyx => new Vector3(Z, Y, X);

        public readonly float LengthSquared
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => AsNVec3(this).LengthSquared();
        }
        public readonly float Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MathF.Sqrt(LengthSquared);
        }

        public readonly bool IsZero
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this == Zero;
        }

        /// <summary>Return true if vector contains NaN, +Infinity or -Infinity. Otherwise false.</summary>
        public readonly bool IsInvalid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => float.IsNaN(X) || float.IsNaN(Y) || float.IsNaN(Z) || float.IsInfinity(X) || float.IsInfinity(Y) || float.IsInfinity(Z);
        }

        /// <summary>Return (<see cref="IsZero"/> || <see cref="IsInvalid"/>)</summary>
        public readonly bool IsZeroOrInvalid
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsZero || IsInvalid;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3(float x, float y, float z) => (X, Y, Z) = (x, y, z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3(float value) => (X, Y, Z) = (value, value, value);

        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out float x, out float y, out float z) => (x, y, z) = (X, Y, Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float SumElement() => X + Y + Z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float Dot(in Vector3 vec) => (this * vec).SumElement();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(in Vector3 vec1, in Vector3 vec2) => vec1.Dot(vec2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector3 Cross(in Vector3 vec) => Cross(this, vec);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Cross(in Vector3 vec1, in Vector3 vec2)
        {
            return AsVector3(
                new NVec3(vec1.Y, vec1.Z, vec1.X) * new NVec3(vec2.Z, vec2.X, vec2.Y) - new NVec3(vec1.Z, vec1.X, vec1.Y) * new NVec3(vec2.Y, vec2.Z, vec2.X)
            );

            //return new Vector3(vec1.Y * vec2.Z - vec1.Z * vec2.Y,
            //                   vec1.Z * vec2.X - vec1.X * vec2.Z,
            //                   vec1.X * vec2.Y - vec1.Y * vec2.X);
        }

        /// <summary>Get angle as radian between two vectors</summary>
        /// <param name="vec1">vector1</param>
        /// <param name="vec2">vector2</param>
        /// <returns>The angle as radian</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AngleBetween(Vector3 vec1, Vector3 vec2)
        {
            var cos = Dot(vec1, vec2) / (vec1.Length * vec2.Length);
            return (cos > 1f) ? 0f :
                   (cos < -1f) ? MathF.PI :
                                 MathF.Acos(cos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CosAngleBetween(Vector3 vec1, Vector3 vec2)
        {
            return Dot(vec1, vec2) / (vec1.Length * vec2.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Normalize()
        {
            ref readonly var nvec = ref AsNVec3(this);
            var len = nvec.Length();
            this = AsVector3(nvec / len);

            //var len = Length;
            //X /= len;
            //Y /= len;
            //Z /= len;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector3 Normalized()
        {
            ref readonly var nvec = ref AsNVec3(this);
            var len = nvec.Length();
            return AsVector3(nvec / len);

            //var len = Length;
            //return new Vector3(X / len, Y / len, Z / len);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Matrix4 ToTranslationMatrix4() => new Matrix4(1, 0, 0, X,
                                                                      0, 1, 0, Y,
                                                                      0, 0, 1, Z,
                                                                      0, 0, 0, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void ToTranslationMatrix4(out Matrix4 dest) => dest = new Matrix4(1, 0, 0, X,
                                                                                          0, 1, 0, Y,
                                                                                          0, 0, 1, Z,
                                                                                          0, 0, 0, 1);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Matrix4 ToScaleMatrix4() => new Matrix4(X, 0, 0, 0,
                                                                0, Y, 0, 0,
                                                                0, 0, Z, 0,
                                                                0, 0, 0, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Matrix4 ToScaleMatrix4(out Matrix4 dest) => dest = new Matrix4(X, 0, 0, 0,
                                                                                       0, Y, 0, 0,
                                                                                       0, 0, Z, 0,
                                                                                       0, 0, 0, 1);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override bool Equals(object? obj) => obj is Vector3 vector && Equals(vector);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Vector3 other) => X == other.X && Y == other.Y && Z == other.Z;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override int GetHashCode() => HashCode.Combine(X, Y, Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override string ToString() => DebuggerDisplay;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator -(in Vector3 vec) => new Vector3(-vec.X, -vec.Y, -vec.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Vector3 left, in Vector3 right) => left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Vector3 left, in Vector3 right) => !(left == right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator +(in Vector3 vec1, in Vector3 vec2)
        {
            return AsVector3(AsNVec3(vec1) + AsNVec3(vec2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator +(in Vector3 vec, float right)
        {
            return AsVector3(AsNVec3(vec) + new NVec3(right));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator +(float left, in Vector3 vec)
        {
            return AsVector3(new NVec3(left) + AsNVec3(vec));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator -(in Vector3 vec1, in Vector3 vec2)
        {
            return AsVector3(AsNVec3(vec1) - AsNVec3(vec2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator -(in Vector3 vec, float right)
        {
            return AsVector3(AsNVec3(vec) - new NVec3(right));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator -(float left, in Vector3 vec)
        {
            return AsVector3(new NVec3(left) - AsNVec3(vec));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator *(in Vector3 vec1, in Vector3 vec2)
        {
            return AsVector3(AsNVec3(vec1) * AsNVec3(vec2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator *(in Vector3 vec, float right)
        {
            return AsVector3(AsNVec3(vec) * new NVec3(right));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator *(float left, in Vector3 vec)
        {
            return AsVector3(new NVec3(left) * AsNVec3(vec));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator /(in Vector3 vec1, in Vector3 vec2)
        {
            return AsVector3(AsNVec3(vec1) / AsNVec3(vec2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator /(in Vector3 vec, float right)
        {
            return AsVector3(AsNVec3(vec) / new NVec3(right));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator /(float left, in Vector3 vec)
        {
            return AsVector3(new NVec3(left) / AsNVec3(vec));
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref readonly NVec3 AsNVec3(in Vector3 vec) => ref Unsafe.As<Vector3, NVec3>(ref Unsafe.AsRef(vec));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref readonly Vector3 AsVector3(in NVec3 vec) => ref Unsafe.As<NVec3, Vector3>(ref Unsafe.AsRef(vec));
    }

    internal static class VectorExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref Vector2 RefXy(ref this Vector3 vec)
        {
            return ref Unsafe.As<Vector3, Vector2>(ref vec);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref Vector2 RefYz(ref this Vector3 vec)
        {
            return ref Unsafe.As<float, Vector2>(ref vec.Y);
        }
    }
}
