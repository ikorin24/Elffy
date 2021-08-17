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
    public struct Vector4 : IEquatable<Vector4>
    {
        [FieldOffset(0)]
        public float X;
        [FieldOffset(4)]
        public float Y;
        [FieldOffset(8)]
        public float Z;
        [FieldOffset(12)]
        public float W;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebuggerDisplay => $"({X}, {Y}, {Z}, {W})";

        public static Vector4 UnitX => new Vector4(1, 0, 0, 0);
        public static Vector4 UnitY => new Vector4(0, 1, 0, 0);
        public static Vector4 UnitZ => new Vector4(0, 0, 1, 0);
        public static Vector4 UnitW => new Vector4(0, 0, 0, 1);
        public static Vector4 Zero => new Vector4(0, 0, 0, 0);
        public static Vector4 One => new Vector4(1, 1, 1, 1);
        public static unsafe int SizeInBytes => sizeof(Vector4);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector2 Xy => new Vector2(X, Y);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector2 Xz => new Vector2(X, Z);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector2 Xw => new Vector2(X, W);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector2 Yx => new Vector2(Y, X);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector2 Yz => new Vector2(Y, Z);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector2 Yw => new Vector2(Y, W);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector2 Zx => new Vector2(Z, X);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector2 Zy => new Vector2(Z, Y);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector2 Zw => new Vector2(Z, W);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector2 Wx => new Vector2(W, X);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector2 Wy => new Vector2(W, Y);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector2 Wz => new Vector2(W, Z);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Xyz => new Vector3(X, Y, Z);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Xyw => new Vector3(X, Y, W);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Xzy => new Vector3(X, Z, Y);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Xzw => new Vector3(X, Z, W);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Xwy => new Vector3(X, W, Y);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Xwz => new Vector3(X, W, Z);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Yxz => new Vector3(Y, X, Z);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Yxw => new Vector3(Y, X, W);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Yzx => new Vector3(Y, Z, X);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Yzw => new Vector3(Y, Z, W);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Ywx => new Vector3(Y, W, X);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Ywz => new Vector3(Y, W, Z);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Zxy => new Vector3(Z, X, Y);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Zxw => new Vector3(Z, X, W);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Zyx => new Vector3(Z, Y, X);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Zyw => new Vector3(Z, Y, W);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Zwx => new Vector3(Z, W, X);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Zwy => new Vector3(Z, W, Y);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Wxy => new Vector3(W, X, Y);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Wxz => new Vector3(W, X, Z);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Wyx => new Vector3(W, Y, X);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Wyz => new Vector3(W, Y, Z);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Wzx => new Vector3(W, Z, X);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector3 Wzy => new Vector3(W, Z, Y);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector4 Xywz => new Vector4(X, Y, W, Z);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector4 Xzyw => new Vector4(X, Z, Y, W);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector4 Xzwy => new Vector4(X, Z, W, Y);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector4 Xwyz => new Vector4(X, W, Y, Z);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector4 Xwzy => new Vector4(X, W, Z, Y);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector4 Yxzw => new Vector4(Y, X, Z, W);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector4 Yxwz => new Vector4(Y, X, W, Z);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector4 Yzxw => new Vector4(Y, Z, X, W);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector4 Yzwx => new Vector4(Y, Z, W, X);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector4 Ywxz => new Vector4(Y, W, X, Z);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector4 Ywzx => new Vector4(Y, W, Z, X);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector4 Zxyw => new Vector4(Z, X, Y, W);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector4 Zxwy => new Vector4(Z, X, W, Y);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector4 Zyxw => new Vector4(Z, Y, X, W);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector4 Zywx => new Vector4(Z, Y, W, X);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector4 Zwxy => new Vector4(Z, W, X, Y);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector4 Zwyx => new Vector4(Z, W, Y, X);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector4 Wxyz => new Vector4(W, X, Y, Z);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector4 Wxzy => new Vector4(W, X, Z, Y);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector4 Wyxz => new Vector4(W, Y, X, Z);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector4 Wyzx => new Vector4(W, Y, Z, X);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector4 Wzxy => new Vector4(W, Z, X, Y);
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector4 Wzyx => new Vector4(W, Z, Y, X);


        public readonly float LengthSquared
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (X * X) + (Y * Y) + (Z * Z) + (W * W);
        }

        public readonly float Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MathF.Sqrt(LengthSquared);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4(float x, float y, float z, float w) => (X, Y, Z, W) = (x, y, z, w);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4(in Vector4 v) => this = v;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4(in Vector3 v) => (X, Y, Z, W) = (v.X, v.Y, v.Z, 0);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4(in Vector3 v, float w) => (X, Y, Z, W) = (v.X, v.Y, v.Z, w);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4(in Vector2 v) => (X, Y, Z, W) = (v.X, v.Y, 0, 0);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4(in Vector2 v, float z, float w) => (X, Y, Z, W) = (v.X, v.Y, z, w);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4(float value) => (X, Y, Z, W) = (value, value, value, value);

        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out float x, out float y, out float z, out float w) => (x, y, z, w) = (X, Y, Z, W);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float SumElement() => X + Y + Z + W;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float Dot(in Vector4 vec) => Mult(this, vec).SumElement();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(in Vector4 vec1, in Vector4 vec2) => vec1.Dot(vec2);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector4 Mult(in Vector4 vec) => new Vector4(X * vec.X, Y * vec.Y, Z * vec.Z, W * vec.W);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Mult(in Vector4 vec1, in Vector4 vec2) => vec1.Mult(vec2);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector4 Normalized()
        {
            var len = Length;
            return new Vector4(X / len, Y / len, Z / len, W / len);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override bool Equals(object? obj) => obj is Vector4 vector && Equals(vector);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Vector4 other) => X == other.X && Y == other.Y && Z == other.Z && W == other.W;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override int GetHashCode() => HashCode.Combine(X, Y, Z, W);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override string ToString() => DebuggerDisplay;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator -(in Vector4 vec) => new Vector4(-vec.X, -vec.Y, -vec.Z, -vec.W);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Vector4 left, in Vector4 right) => left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Vector4 left, in Vector4 right) => !(left == right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator +(in Vector4 vec1, in Vector4 vec2) => new Vector4(vec1.X + vec2.X, vec1.Y + vec2.Y, vec1.Z + vec2.Z, vec1.W + vec2.W);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator +(in Vector4 vec1, float right) => new Vector4(vec1.X + right, vec1.Y + right, vec1.Z + right, vec1.W + right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator -(in Vector4 vec1, in Vector4 vec2) => new Vector4(vec1.X - vec2.X, vec1.Y - vec2.Y, vec1.Z - vec2.Z, vec1.W - vec2.W);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator -(in Vector4 vec1, float right) => new Vector4(vec1.X - right, vec1.Y - right, vec1.Z - right, vec1.W - right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator *(in Vector4 vec1, float right) => new Vector4(vec1.X * right, vec1.Y * right, vec1.Z * right, vec1.W * right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator *(float right, in Vector4 vec1) => new Vector4(vec1.X * right, vec1.Y * right, vec1.Z * right, vec1.W * right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator /(in Vector4 vec1, float right) => new Vector4(vec1.X / right, vec1.Y / right, vec1.Z / right, vec1.W / right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 operator /(float right, in Vector4 vec1) => new Vector4(vec1.X / right, vec1.Y / right, vec1.Z / right, vec1.W / right);
    }
}
