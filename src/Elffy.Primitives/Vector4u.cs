#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel;

namespace Elffy
{
    [StructLayout(LayoutKind.Explicit)]
    [DebuggerDisplay("{DebuggerDisplay}")]
    public struct Vector4u : IEquatable<Vector4u>
    {
        [FieldOffset(0)]
        public uint X;
        [FieldOffset(4)]
        public uint Y;
        [FieldOffset(8)]
        public uint Z;
        [FieldOffset(12)]
        public uint W;

        public ref uint this[int index]
        {
            [UnscopedRef]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if((uint)index >= 4) { throw new IndexOutOfRangeException(nameof(index)); }
                return ref Unsafe.Add(ref X, index);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebuggerDisplay => $"({X}, {Y}, {Z}, {W})";

        public static Vector4u UnitX => new Vector4u(1, 0, 0, 0);
        public static Vector4u UnitY => new Vector4u(0, 1, 0, 0);
        public static Vector4u UnitZ => new Vector4u(0, 0, 1, 0);
        public static Vector4u UnitW => new Vector4u(0, 0, 0, 1);
        public static Vector4u Zero => new Vector4u(0, 0, 0, 0);
        public static Vector4u One => new Vector4u(1, 1, 1, 1);
        public static unsafe int SizeInBytes => sizeof(Vector4u);

        public readonly bool IsZero => this == default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4u(uint x, uint y, uint z, uint w) => (X, Y, Z, W) = (x, y, z, w);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector4u(uint value)
        {
            X = value;
            Y = value;
            Z = value;
            W = value;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Destructor(out uint x, out uint y, out uint z, out uint w) => (x, y, z, w) = (X, Y, Z, W);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly uint SumElements() => X + Y + Z + W;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector4 ToVector4() => new Vector4((float)X, (float)Y, (float)Z, (float)W);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector4i ToVector4i() => new Vector4i((int)X, (int)Y, (int)Z, (int)W);

        public readonly override bool Equals(object? obj) => obj is Vector4u u && Equals(u);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Vector4u other)
        {
            return Unsafe.As<Vector4u, UInt128>(ref Unsafe.AsRef(in this)) == Unsafe.As<Vector4u, UInt128>(ref Unsafe.AsRef(in other));
        }

        public readonly override int GetHashCode() => Unsafe.As<Vector4u, UInt128>(ref Unsafe.AsRef(in this)).GetHashCode();

        public readonly override string ToString() => DebuggerDisplay;


        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static Vector4u operator -(in Vector4u vec) => new Vector4l(-vec.X, -vec.Y, -vec.Z, -vec.W);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Vector4u left, in Vector4u right) => left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Vector4u left, in Vector4u right) => !(left == right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4u operator +(in Vector4u vec1, in Vector4u vec2) => new Vector4u(vec1.X + vec2.X, vec1.Y + vec2.Y, vec1.Z + vec2.Z, vec1.W + vec2.W);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4u operator +(in Vector4u vec, uint right) => new Vector4u(vec.X + right, vec.Y + right, vec.Z + right, vec.W + right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4u operator +(uint left, in Vector4u vec) => new Vector4u(left + vec.X, left + vec.Y, left + vec.Z, left + vec.W);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4u operator -(in Vector4u vec1, in Vector4u vec2) => new Vector4u(vec1.X - vec2.X, vec1.Y - vec2.Y, vec1.Z - vec2.Z, vec1.W - vec2.W);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4u operator -(in Vector4u vec, uint right) => new Vector4u(vec.X - right, vec.Y - right, vec.Z - right, vec.W - right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4u operator -(uint left, in Vector4u vec) => new Vector4u(left - vec.X, left - vec.Y, left - vec.Z, left - vec.W);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4u operator *(in Vector4u vec1, in Vector4u vec2) => new Vector4u(vec1.X * vec2.X, vec1.Y * vec2.Y, vec1.Z * vec2.Z, vec1.W * vec2.W);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4u operator *(in Vector4u vec, uint right) => new Vector4u(vec.X * right, vec.Y * right, vec.Z * right, vec.W * right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4u operator *(uint left, in Vector4u vec) => new Vector4u(left * vec.X, left * vec.Y, left * vec.Z, left * vec.W);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4u operator /(in Vector4u vec1, in Vector4u vec2) => new Vector4u(vec1.X / vec2.X, vec1.Y / vec2.Y, vec1.Z / vec2.Z, vec1.W / vec2.W);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4u operator /(in Vector4u vec, uint right) => new Vector4u(vec.X / right, vec.Y / right, vec.Z / right, vec.W / right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4u operator /(uint left, in Vector4u vec) => new Vector4u(left / vec.X, left / vec.Y, left / vec.Z, left / vec.W);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector4u(in Vector4 vec) => new Vector4u((uint)vec.X, (uint)vec.Y, (uint)vec.Z, (uint)vec.W);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector4(in Vector4u vec) => new Vector4(vec.X, vec.Y, vec.Z, vec.W);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector4u(in Vector4i vec) => new Vector4u((uint)vec.X, (uint)vec.Y, (uint)vec.Z, (uint)vec.W);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector4i(in Vector4u vec) => new Vector4i((int)vec.X, (int)vec.Y, (int)vec.Z, (int)vec.W);
    }
}
