#nullable enable
using Cysharp.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using TKVector2i = OpenToolkit.Mathematics.Vector2i;

namespace Elffy
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Vector2i : IEquatable<Vector2i>
    {
        [FieldOffset(0)]
        public int X;
        [FieldOffset(4)]
        public int Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2i(int x, int y) => (X, Y) = (x, y);

        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out int x, out int y) => (x, y) = (X, Y);

        public override bool Equals(object? obj) => obj is Vector2i i && Equals(i);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector2i other) => X == other.X && Y == other.Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => HashCode.Combine(X, Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override string ToString() => ZString.Concat('(', X, ' ', Y, ')');

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Vector2i left, in Vector2i right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Vector2i left, in Vector2i right) => !(left == right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator TKVector2i(Vector2i vec) => Unsafe.As<Vector2i, TKVector2i>(ref vec);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2i(TKVector2i vec) => Unsafe.As<TKVector2i, Vector2i>(ref vec);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2i(in Point point) => new Vector2i(point.X, point.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator Point(in Vector2i vec) => new Point(vec.X, vec.Y);
    }
}
