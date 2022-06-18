#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Elffy.Markup;

namespace Elffy
{
    [DebuggerDisplay("{DebugDisplay}")]
    [StructLayout(LayoutKind.Explicit)]
    [UseLiteralMarkup]
    [LiteralMarkupPattern(LiteralPattern, LiteralEmit)]
    public struct RectI : IEquatable<RectI>
    {
        private const string LiteralPattern = @$"^(?<x>{RegexPatterns.Int}), *(?<y>{RegexPatterns.Int}), *(?<wi>{RegexPatterns.Int}), *(?<he>{RegexPatterns.Int})$";
        private const string LiteralEmit = @"new global::Elffy.RectI((int)(${x}), (int)(${y}), (int)(${wi}), (int)(${he}))";

        [FieldOffset(0)]
        public int X;
        [FieldOffset(4)]
        public int Y;
        [FieldOffset(8)]
        public int Width;
        [FieldOffset(12)]
        public int Height;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebugDisplay => $"({X}, {Y}, {Width}, {Height})";

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

        public bool Contains(Vector2i pos) => ((uint)(pos.X - X) < Width) && ((uint)(pos.Y - Y) < Height);

        public readonly override bool Equals(object? obj) => obj is RectI i && Equals(i);

        public readonly bool Equals(RectI other) => X == other.X &&
                                                    Y == other.Y &&
                                                    Width == other.Width &&
                                                    Height == other.Height;

        public readonly override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

        public static bool operator ==(in RectI left, in RectI right) => left.Equals(right);

        public static bool operator !=(in RectI left, in RectI right) => !(left == right);

        public override string ToString() => DebugDisplay;
    }
}
