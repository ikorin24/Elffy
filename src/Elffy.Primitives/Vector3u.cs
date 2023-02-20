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
    public struct Vector3u : IEquatable<Vector3u>
    {
        [FieldOffset(0)]
        public uint X;
        [FieldOffset(4)]
        public uint Y;
        [FieldOffset(8)]
        public uint Z;

        public ref uint this[int index]
        {
            [UnscopedRef]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if((uint)index >= 3) { throw new IndexOutOfRangeException(nameof(index)); }
                return ref Unsafe.Add(ref X, index);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebuggerDisplay => $"({X}, {Y}, {Z})";

        public static Vector3u UnitX => new Vector3u(1, 0, 0);
        public static Vector3u UnitY => new Vector3u(0, 1, 0);
        public static Vector3u UnitZ => new Vector3u(0, 0, 1);
        public static Vector3u Zero => new Vector3u(0, 0, 0);
        public static Vector3u One => new Vector3u(1, 1, 1);
        public static unsafe int SizeInBytes => sizeof(Vector3u);

        public readonly bool IsZero => this == default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3u(uint x, uint y, uint z) => (X, Y, Z) = (x, y, z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3u(uint value)
        {
            X = value;
            Y = value;
            Z = value;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out uint x, out uint y, out uint z) => (x, y, z) = (X, Y, Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly uint SumElements() => X + Y + Z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector3 ToVector3() => new Vector3((float)X, (float)Y, (float)Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector3i ToVector3i() => new Vector3i((int)X, (int)Y, (int)Z);

        public readonly override bool Equals(object? obj) => obj is Vector3u i && Equals(i);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Vector3u other) => X == other.X && Y == other.Y && Z == other.Z;

        public readonly override int GetHashCode() => HashCode.Combine(X, Y, Z);

        public readonly override string ToString() => DebuggerDisplay;

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //public static Vector3u operator -(in Vector3u vec) => new Vector3l(-vec.X, -vec.Y, -vec.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Vector3u left, in Vector3u right) => left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Vector3u left, in Vector3u right) => !(left == right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3u operator +(in Vector3u vec1, in Vector3u vec2) => new Vector3u(vec1.X + vec2.X, vec1.Y + vec2.Y, vec1.Z + vec2.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3u operator +(in Vector3u vec, uint right) => new Vector3u(vec.X + right, vec.Y + right, vec.Z + right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3u operator +(uint left, in Vector3u vec) => new Vector3u(left + vec.X, left + vec.Y, left + vec.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3u operator -(in Vector3u vec1, in Vector3u vec2) => new Vector3u(vec1.X - vec2.X, vec1.Y - vec2.Y, vec1.Z - vec2.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3u operator -(in Vector3u vec1, uint right) => new Vector3u(vec1.X - right, vec1.Y - right, vec1.Z - right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3u operator -(uint left, in Vector3u vec) => new Vector3u(left - vec.X, left - vec.Y, left - vec.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3u operator *(in Vector3u vec1, in Vector3u vec2) => new Vector3u(vec1.X * vec2.X, vec1.Y * vec2.Y, vec1.Z * vec2.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3u operator *(in Vector3u vec, uint right) => new Vector3u(vec.X * right, vec.Y * right, vec.Z * right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3u operator *(uint left, in Vector3u vec) => new Vector3u(left * vec.X, left * vec.Y, left * vec.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3u operator /(in Vector3u vec1, in Vector3u vec2) => new Vector3u(vec1.X / vec2.X, vec1.Y / vec2.Y, vec1.Z / vec2.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3u operator /(in Vector3u vec1, uint right) => new Vector3u(vec1.X / right, vec1.Y / right, vec1.Z / right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3u operator /(uint left, in Vector3u vec1) => new Vector3u(left / vec1.X, left / vec1.Y, left / vec1.Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector3u(in Vector3 vec) => new Vector3u((uint)vec.X, (uint)vec.Y, (uint)vec.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector3(in Vector3u vec) => new Vector3(vec.X, vec.Y, vec.Z);
    }
}
