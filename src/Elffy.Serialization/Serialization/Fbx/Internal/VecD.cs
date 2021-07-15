#nullable enable
using System;
using System.Diagnostics;

namespace Elffy.Serialization.Fbx.Internal
{
    [DebuggerDisplay("{DebugDisplay(),nq}")]
    internal struct VecD3 : IEquatable<VecD3>
    {
        public double X;
        public double Y;
        public double Z;

        public VecD3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        private string DebugDisplay() => $"({X}, {Y}, {Z})";

        public override bool Equals(object? obj) => obj is VecD3 d && Equals(d);

        public bool Equals(VecD3 other) => X == other.X && Y == other.Y && Z == other.Z;

        public override int GetHashCode() => HashCode.Combine(X, Y, Z);

        public override string ToString() => DebugDisplay();

        public static bool operator ==(VecD3 left, VecD3 right) => left.Equals(right);

        public static bool operator !=(VecD3 left, VecD3 right) => !(left == right);

        public static explicit operator Vector3(in VecD3 vec) => new Vector3((float)vec.X, (float)vec.Y, (float)vec.Z);
    }

    [DebuggerDisplay("{DebugDisplay(),nq}")]
    internal struct VecD2 : IEquatable<VecD2>
    {
        public double X;
        public double Y;

        public VecD2(double x, double y)
        {
            X = x;
            Y = y;
        }

        private string DebugDisplay() => $"({X}, {Y})";

        public override bool Equals(object? obj) => obj is VecD2 d && Equals(d);

        public bool Equals(VecD2 other) => X == other.X && Y == other.Y;

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public override string ToString() => DebugDisplay();

        public static bool operator ==(VecD2 left, VecD2 right) => left.Equals(right);

        public static bool operator !=(VecD2 left, VecD2 right) => !(left == right);

        public static explicit operator Vector2(in VecD2 vec) => new Vector2((float)vec.X, (float)vec.Y);
    }
}
