﻿#nullable enable
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Elffy.Markup;
using NVec2 = System.Numerics.Vector2;
using System.Diagnostics.CodeAnalysis;

namespace Elffy
{
    [StructLayout(LayoutKind.Explicit)]
    [DebuggerDisplay("{DebuggerDisplay}")]
    [UseLiteralMarkup]
    [LiteralMarkupPattern(LiteralPattern, LiteralEmit)]
    public struct Vector2 : IEquatable<Vector2>
    {
        private const string LiteralPattern = @$"^(?<x>{RegexPatterns.Float}), *(?<y>{RegexPatterns.Float})$";
        private const string LiteralEmit = @"new global::Elffy.Vector2((float)(${x}), (float)(${y}))";

        [FieldOffset(0)]
        public float X;
        [FieldOffset(4)]
        public float Y;

        public ref float this[int index]
        {
            [UnscopedRef]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if((uint)index >= 2) { throw new IndexOutOfRangeException(nameof(index)); }
                return ref Unsafe.Add(ref X, index);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebuggerDisplay => $"({X}, {Y})";

        public static Vector2 UnitX => new Vector2(1, 0);
        public static Vector2 UnitY => new Vector2(0, 1);
        public static Vector2 Zero => new Vector2(0, 0);
        public static Vector2 One => new Vector2(1, 1);
        public static Vector2 MaxValue => new Vector2(float.MaxValue, float.MaxValue);
        public static Vector2 MinValue => new Vector2(float.MinValue, float.MinValue);
        public static unsafe int SizeInBytes => sizeof(Vector2);

        public readonly bool IsZero => this == default;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public readonly Vector2 Yx => new Vector2(Y, X);

        public readonly float LengthSquared
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => AsNVec2(this).LengthSquared();
        }
        public readonly float Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MathF.Sqrt(LengthSquared);
        }

        /// <summary>Return true if vector contains NaN, +Infinity or -Infinity. Otherwise false.</summary>
        public readonly bool ContainsNaNOrInfinity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => float.IsNaN(X) || float.IsNaN(Y) || float.IsInfinity(X) || float.IsInfinity(Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2(float x, float y) => (X, Y) = (x, y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2(float value) => (X, Y) = (value, value);

        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out float x, out float y) => (x, y) = (X, Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float SumElements() => X + Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float Dot(in Vector2 vec)
        {
            return NVec2.Dot(AsNVec2(this), AsNVec2(vec));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(in Vector2 vec1, in Vector2 vec2)
        {
            return NVec2.Dot(AsNVec2(vec1), AsNVec2(vec2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Mix(in Vector2 vec1, in Vector2 vec2, float a)
        {
            return vec1 * (1f - a) + vec2 * a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Min(in Vector2 vec1, in Vector2 vec2)
        {
            return new Vector2
            {
                X = MathF.Min(vec1.X, vec2.X),
                Y = MathF.Min(vec1.Y, vec2.Y),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Max(in Vector2 vec1, in Vector2 vec2)
        {
            return new Vector2
            {
                X = MathF.Max(vec1.X, vec2.X),
                Y = MathF.Max(vec1.Y, vec2.Y),
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector2 Normalized()
        {
            return AsVector2(AsNVec2(this) / AsNVec2(this).Length());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Normalize()
        {
            this = Normalized();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Vector2i ToVector2i() => new Vector2i((int)X, (int)Y);

        public readonly override bool Equals(object? obj) => obj is Vector2 vector && Equals(vector);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Vector2 other) => X == other.X && Y == other.Y;
        public readonly override int GetHashCode() => HashCode.Combine(X, Y);
        public readonly override string ToString() => DebuggerDisplay;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator -(in Vector2 vec) => new Vector2(-vec.X, -vec.Y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in Vector2 left, in Vector2 right) => left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in Vector2 left, in Vector2 right) => !(left == right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator +(in Vector2 vec1, in Vector2 vec2)
        {
            return AsVector2(AsNVec2(vec1) + AsNVec2(vec2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator +(in Vector2 vec, float right)
        {
            return AsVector2(AsNVec2(vec) + new NVec2(right));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator +(float left, in Vector2 vec)
        {
            return AsVector2(new NVec2(left) + AsNVec2(vec));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator -(in Vector2 vec1, in Vector2 vec2)
        {
            return AsVector2(AsNVec2(vec1) - AsNVec2(vec2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator -(in Vector2 vec, float right)
        {
            return AsVector2(AsNVec2(vec) - new NVec2(right));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator -(float left, in Vector2 vec)
        {
            return AsVector2(new NVec2(left) - AsNVec2(vec));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator *(in Vector2 vec1, in Vector2 vec2)
        {
            return AsVector2(AsNVec2(vec1) * AsNVec2(vec2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator *(in Vector2 vec, float right)
        {
            return AsVector2(AsNVec2(vec) * right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator *(float left, in Vector2 vec)
        {
            return AsVector2(left * AsNVec2(vec));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator /(in Vector2 vec1, in Vector2 vec2)
        {
            return AsVector2(AsNVec2(vec1) / AsNVec2(vec2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator /(in Vector2 vec, float right)
        {
            return AsVector2(AsNVec2(vec) / right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator /(float left, in Vector2 vec)
        {
            return AsVector2(new NVec2(left) / AsNVec2(vec));
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref readonly NVec2 AsNVec2(in Vector2 vec) => ref Unsafe.As<Vector2, NVec2>(ref Unsafe.AsRef(vec));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref readonly Vector2 AsVector2(in NVec2 vec) => ref Unsafe.As<NVec2, Vector2>(ref Unsafe.AsRef(vec));
    }
}
