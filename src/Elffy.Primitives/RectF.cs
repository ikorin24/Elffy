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
    public struct RectF : IEquatable<RectF>
    {
        private const string LiteralPattern = @$"^(?<x>{RegexPatterns.Float}), *(?<y>{RegexPatterns.Float}), *(?<wi>{RegexPatterns.Float}), *(?<he>{RegexPatterns.Float})$";
        private const string LiteralEmit = @"new global::Elffy.RectF((float)(${x}), (float)(${y}), (float)(${wi}), (float)(${he}))";

        [FieldOffset(0)]
        public float X;
        [FieldOffset(4)]
        public float Y;
        [FieldOffset(8)]
        public float Width;
        [FieldOffset(12)]
        public float Height;

        public Vector2 Position => new Vector2(X, Y);
        public Vector2 Size => new Vector2(Width, Height);

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebugDisplay => $"({X}, {Y}, {Width}, {Height})";

        public RectF(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public RectF(in Vector2 pos, in Vector2 size)
        {
            X = pos.X;
            Y = pos.Y;
            Width = size.X;
            Height = size.Y;
        }

        public bool Contains(Vector2 pos)
        {
            return (X <= pos.X) && (pos.X < X + Width) &&
                   (Y <= pos.Y) && (pos.Y < Y + Height);
        }

        public override string ToString() => DebugDisplay;

        public override bool Equals(object? obj) => obj is RectF f && Equals(f);

        public bool Equals(RectF other) => X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;

        public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

        public static bool operator ==(RectF left, RectF right) => left.Equals(right);

        public static bool operator !=(RectF left, RectF right) => !(left == right);
    }
}
