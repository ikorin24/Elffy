#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace Elffy
{
    public readonly struct PlainEquation : IEquatable<PlainEquation>
    {
        // [equation of plain]
        // nx*x + ny*y + nz*z + d = 0

        // Don't change layout of fileds. It must have same layout as Vector4 for SIMD.
        public readonly Vector3 Normal; // normalized
        public readonly float D;

        private PlainEquation(in Vector3 normal, float d)
        {
            Normal = normal;
            D = d;
        }

        public static PlainEquation FromTriangle(in Vector3 p0, in Vector3 p1, in Vector3 p2)
        {
            var n = Vector3.Cross(p2 - p1, p0 - p1).Normalized();
            return new PlainEquation(n, -n.Dot(p1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsLineCrossing(in Vector3 from, in Vector3 to)
        {
            return (Normal.Dot(from) + D >= 0) ^ (Normal.Dot(to) + D >= 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAbove(in Vector3 pos) => Normal.Dot(pos) + D >= 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetSignedDistance(in Vector3 pos) => Normal.Dot(pos) + D;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetAbsDistance(in Vector3 pos) => MathF.Abs(Normal.Dot(pos) + D);

        public override string ToString()
        {
            var d = (D < 0) ? $"- {-D}" : $"+ {D}";
            return $"plain: {Normal.X}X + {Normal.Y}Y + {Normal.Z}Z {d} = 0";
        }

        public override bool Equals(object? obj) => obj is PlainEquation equation && Equals(equation);

        public bool Equals(PlainEquation other) => Normal.Equals(other.Normal) && D == other.D;

        public override int GetHashCode() => HashCode.Combine(Normal, D);
    }
}
