#nullable enable
using Elffy.Effective;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy
{
    [StructLayout(LayoutKind.Explicit)]
    public struct RectI : IEquatable<RectI>
    {
        [FieldOffset(0)]
        public int X;
        [FieldOffset(4)]
        public int Y;
        [FieldOffset(8)]
        public int Width;
        [FieldOffset(12)]
        public int Height;

        public RectI(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public RectI(in Vector2i pos, in Vector2i size)
        {
            X = pos.X;
            Y = pos.Y;
            Width = size.X;
            Height = size.Y;
        }

        public readonly override bool Equals(object? obj) => obj is RectI i && Equals(i);

        public readonly bool Equals(RectI other) => X == other.X &&
                                                    Y == other.Y &&
                                                    Width == other.Width &&
                                                    Height == other.Height;

        public readonly override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

        public static bool operator ==(in RectI left, in RectI right) => left.Equals(right);

        public static bool operator !=(in RectI left, in RectI right) => !(left == right);
    }

    public static class RectIExtension
    {
        public static ref Vector2i Position(this ref RectI source)
        {
            return ref Unsafe.As<int, Vector2i>(ref source.X);
        }

        public static ref Vector2i Size(this ref RectI source)
        {
            return ref Unsafe.As<int, Vector2i>(ref source.Width);
        }
    }
}
