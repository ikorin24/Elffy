#nullable enable
using Cysharp.Text;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Elffy.Effective;
using TKVector3i = OpenTK.Mathematics.Vector3i;

namespace Elffy
{
    [StructLayout(LayoutKind.Explicit)]
    [DebuggerDisplay("{DebuggerDisplay}")]
    public struct Vector3i : IEquatable<Vector3i>
    {
        [FieldOffset(0)]
        public int X;
        [FieldOffset(4)]
        public int Y;
        [FieldOffset(8)]
        public int Z;

        private readonly string DebuggerDisplay => ToString();

        public static readonly Vector3i UnitX = new Vector3i(1, 0, 0);
        public static readonly Vector3i UnitY = new Vector3i(0, 1, 0);
        public static readonly Vector3i UnitZ = new Vector3i(0, 0, 1);
        public static readonly Vector3i Zero = new Vector3i(0, 0, 0);
        public static readonly Vector3i One = new Vector3i(1, 1, 1);
        public static unsafe readonly int SizeInBytes = sizeof(Vector3i);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector3i(int x, int y, int z) => (X, Y, Z) = (x, y, z);

        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out int x, out int y, out int z) => (x, y, z) = (X, Y, Z);

        public readonly override bool Equals(object? obj) => obj is Vector3i i && Equals(i);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Vector3i other) => X == other.X && Y == other.Y && Z == other.Z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override int GetHashCode() => HashCode.Combine(X, Y, Z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override string ToString() => ZString.Concat('(', X, ", ", Y, ", ", Z, ')');

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator -(in Vector3i vec) => new Vector3i(-vec.X, -vec.Y, -vec.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Vector3i left, in Vector3i right) => left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Vector3i left, in Vector3i right) => !(left == right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator +(in Vector3i vec1, in Vector3i vec2) => new Vector3i(vec1.X + vec2.X, vec1.Y + vec2.Y, vec1.Z + vec2.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator +(in Vector3i vec1, int right) => new Vector3i(vec1.X + right, vec1.Y + right, vec1.Z + right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator -(in Vector3i vec1, in Vector3i vec2) => new Vector3i(vec1.X - vec2.X, vec1.Y - vec2.Y, vec1.Z - vec2.Z);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator -(in Vector3i vec1, int right) => new Vector3i(vec1.X - right, vec1.Y - right, vec1.Z - right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator *(in Vector3i vec1, int right) => new Vector3i(vec1.X * right, vec1.Y * right, vec1.Z * right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator *(int right, in Vector3i vec1) => new Vector3i(vec1.X * right, vec1.Y * right, vec1.Z * right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator /(in Vector3i vec1, int right) => new Vector3i(vec1.X / right, vec1.Y / right, vec1.Z / right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3i operator /(int right, in Vector3i vec1) => new Vector3i(vec1.X / right, vec1.Y / right, vec1.Z / right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator TKVector3i(in Vector3i vec) => UnsafeEx.As<Vector3i, TKVector3i>(in vec);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector3i(in TKVector3i vec) => UnsafeEx.As<TKVector3i, Vector3i>(in vec);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Vector3i(in Vector3 vec) => new Vector3i((int)vec.X, (int)vec.Y, (int)vec.Z);
    }
}
