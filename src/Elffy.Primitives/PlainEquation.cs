#nullable enable
using System;

namespace Elffy
{
    public readonly struct PlainEquation : IEquatable<PlainEquation>
    {
        // [equation of plain]
        // nx*x + ny*y + nz*z - d = 0

        public readonly Vector3 Normal; // normalized
        private readonly float _d;

        private PlainEquation(in Vector3 normal, float d)
        {
            Normal = normal;
            _d = d;
        }

        public static PlainEquation FromTriangle(in Vector3 p0, in Vector3 p1, in Vector3 p2)
        {
            var n = Vector3.Cross(p2 - p1, p0 - p1).Normalized();
            return new PlainEquation(n, n.Dot(p1));
        }

        public float GetSignedDistance(in Vector3 pos) => Normal.Dot(pos) - _d;

        public float GetDistance(in Vector3 pos) => MathF.Abs(Normal.Dot(pos) - _d);

        public override string ToString()
        {
            return $"plain: {Normal.X}X + {Normal.Y}Y + {Normal.Z}Z = {_d}";
        }

        public override bool Equals(object? obj) => obj is PlainEquation equation && Equals(equation);

        public bool Equals(PlainEquation other) => Normal.Equals(other.Normal) && _d == other._d;

        public override int GetHashCode() => HashCode.Combine(Normal, _d);
    }
}
