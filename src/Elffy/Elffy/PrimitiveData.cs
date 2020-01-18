#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TKVector2 = OpenTK.Vector2;
using TKVector3 = OpenTK.Vector3;
using TKVector4 = OpenTK.Vector4;
using TKColor4 = OpenTK.Graphics.Color4;

namespace Elffy.DoNotUse_NowDeveloping         // TODO:
{
    [StructLayout(LayoutKind.Explicit)]
    public struct Vector2 : IEquatable<Vector2>
    {
        [FieldOffset(0)]
        public float X;
        [FieldOffset(4)]
        public float Y;

        public static readonly Vector2 UnitX = new Vector2(1, 0);
        public static readonly Vector2 UnitY = new Vector2(0, 1);
        public static readonly Vector2 Zero =  new Vector2(0, 0);
        public static readonly Vector2 One =   new Vector2(1, 1);
        public static unsafe readonly int SizeInBytes = sizeof(Vector2);

        public Vector2 Yx => new Vector2(Y, X);

        public float LengthSquared => (X * X) + (Y * Y);
        public float Length => (float)Math.Sqrt(LengthSquared);

        public Vector2(float x, float y) => (X, Y) = (x, y);
        public Vector2(float value) => (X, Y) = (value, value);

        public Vector2 Dot(Vector2 vec) => this * vec;
        public static Vector2 Dot(Vector2 vec1, Vector2 vec2) => vec1 * vec2;
        public override bool Equals(object? obj) => obj is Vector2 vector && Equals(vector);
        public bool Equals(Vector2 other) => X == other.X && Y == other.Y;
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public override string ToString() => $"({X}, {Y})";
        public static bool operator ==(Vector2 left, Vector2 right) => left.Equals(right);
        public static bool operator !=(Vector2 left, Vector2 right) => !(left == right);
        public static Vector2 operator +(Vector2 vec1, Vector2 vec2) => new Vector2(vec1.X + vec2.X, vec1.Y + vec2.Y);
        public static Vector2 operator +(Vector2 vec1, float right) => new Vector2(vec1.X + right, vec1.Y + right);
        public static Vector2 operator -(Vector2 vec1, Vector2 vec2) => new Vector2(vec1.X - vec2.X, vec1.Y - vec2.Y);
        public static Vector2 operator -(Vector2 vec1, float right) => new Vector2(vec1.X - right, vec1.Y - right);
        public static Vector2 operator *(Vector2 vec1, Vector2 vec2) => new Vector2(vec1.X * vec2.X, vec1.Y * vec2.Y);
        public static Vector2 operator *(Vector2 vec1, float right) => new Vector2(vec1.X * right, vec1.Y * right);
        public static Vector2 operator /(Vector2 vec1, float right) => new Vector2(vec1.X / right, vec1.Y / right);
        public static implicit operator TKVector2(Vector2 vec) => Unsafe.As<Vector2, TKVector2>(ref vec);
        public static implicit operator Vector2(TKVector2 vec) => Unsafe.As<TKVector2, Vector2>(ref vec);
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Vector3 : IEquatable<Vector3>
    {
        [FieldOffset(0)]
        public float X;
        [FieldOffset(4)]
        public float Y;
        [FieldOffset(8)]
        public float Z;

        public static readonly Vector3 UnitX = new Vector3(1, 0, 0);
        public static readonly Vector3 UnitY = new Vector3(0, 1, 0);
        public static readonly Vector3 UnitZ = new Vector3(0, 0, 1);
        public static readonly Vector3 Zero =  new Vector3(0, 0, 0);
        public static readonly Vector3 One =   new Vector3(1, 1, 1);
        public static unsafe readonly int SizeInBytes = sizeof(Vector3);

        public Vector2 Xy => new Vector2(X, Y);
        public Vector2 Xz => new Vector2(X, Z);
        public Vector2 Yx => new Vector2(Y, X);
        public Vector2 Yz => new Vector2(Y, Z);
        public Vector2 Zx => new Vector2(Z, X);
        public Vector2 Zy => new Vector2(Z, Y);
        public Vector3 Xzy => new Vector3(X, Z, Y);
        public Vector3 Yxz => new Vector3(Y, X, Z);
        public Vector3 Yzx => new Vector3(Y, Z, X);
        public Vector3 Zxy => new Vector3(Z, X, Y);
        public Vector3 Zyx => new Vector3(Z, Y, X);

        public float LengthSquared => (X * X) + (Y * Y) + (Z * Z);
        public float Length => (float)Math.Sqrt(LengthSquared);
        
        public Vector3(float x, float y, float z) => (X, Y, Z) = (x, y, z);
        public Vector3(Vector3 v) => this = v;
        public Vector3(Vector2 v) => (X, Y, Z) = (v.X, v.Y, 0);
        public Vector3(Vector2 v, float z) => (X, Y, Z) = (v.X, v.Y, z);
        public Vector3(Vector4 v) => (X, Y, Z) = (v.X, v.Y, v.Z);
        public Vector3(float value) => (X, Y, Z) = (value, value, value);

        public Vector3 Dot(Vector3 vec) => this * vec;
        public static Vector3 Dot(Vector3 vec1, Vector3 vec2) => vec1 * vec2;

        public Vector3 Cross(Vector3 vec) => Cross(this, vec);
        public static Vector3 Cross(Vector3 vec1, Vector3 vec2) => new Vector3(vec1.Y * vec2.Z - vec1.Z * vec2.Y,
                                                                               vec1.Z * vec2.X - vec1.X * vec2.Z,
                                                                               vec1.X * vec2.Y - vec1.Y * vec2.X);
        public override bool Equals(object? obj) => obj is Vector3 vector && Equals(vector);
        public bool Equals(Vector3 other) => X == other.X && Y == other.Y && Z == other.Z;
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
        public override string ToString() => $"({X}, {Y}, {Z})";
        public static bool operator ==(Vector3 left, Vector3 right) => left.Equals(right);
        public static bool operator !=(Vector3 left, Vector3 right) => !(left == right);

        public static Vector3 operator +(Vector3 vec1, Vector3 vec2) => new Vector3(vec1.X + vec2.X, vec1.Y + vec2.Y, vec1.Z + vec2.Z);
        public static Vector3 operator +(Vector3 vec1, float right) => new Vector3(vec1.X + right, vec1.Y + right, vec1.Z + right);
        public static Vector3 operator -(Vector3 vec1, Vector3 vec2) => new Vector3(vec1.X - vec2.X, vec1.Y - vec2.Y, vec1.Z - vec2.Z);
        public static Vector3 operator -(Vector3 vec1, float right) => new Vector3(vec1.X - right, vec1.Y - right, vec1.Z - right);
        public static Vector3 operator *(Vector3 vec1, Vector3 vec2) => new Vector3(vec1.X * vec2.X, vec1.Y * vec2.Y, vec1.Z * vec2.Z);
        public static Vector3 operator *(Vector3 vec1, float right) => new Vector3(vec1.X * right, vec1.Y * right, vec1.Z * right);
        public static Vector3 operator /(Vector3 vec1, float right) => new Vector3(vec1.X / right, vec1.Y / right, vec1.Z / right);

        public static implicit operator TKVector3(Vector3 vec) => Unsafe.As<Vector3, TKVector3>(ref vec);
        public static implicit operator Vector3(TKVector3 vec) => Unsafe.As<TKVector3, Vector3>(ref vec);
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Vector4 : IEquatable<Vector4>
    {
        [FieldOffset(0)]
        public float X;
        [FieldOffset(4)]
        public float Y;
        [FieldOffset(8)]
        public float Z;
        [FieldOffset(12)]
        public float W;

        public static readonly Vector4 UnitX = new Vector4(1, 0, 0, 0);
        public static readonly Vector4 UnitY = new Vector4(0, 1, 0, 0);
        public static readonly Vector4 UnitZ = new Vector4(0, 0, 1, 0);
        public static readonly Vector4 UnitW = new Vector4(0, 0, 0, 1);
        public static readonly Vector4 Zero =  new Vector4(0, 0, 0, 0);
        public static readonly Vector4 One =   new Vector4(1, 1, 1, 1);
        public static unsafe readonly int SizeInBytes = sizeof(Vector4);

        public float LengthSquared => (X * X) + (Y * Y) + (Z * Z) + (W * W);
        public float Length => (float)Math.Sqrt(LengthSquared);
        public Vector4(float x, float y, float z, float w) => (X, Y, Z, W) = (x, y, z, w);
        public Vector4(Vector4 v) => this = v;
        public Vector4(Vector3 v) => (X, Y, Z, W) = (v.X, v.Y, v.Z, 0);
        public Vector4(Vector2 v) => (X, Y, Z, W) = (v.X, v.Y, 0, 0);
        public Vector4(float value) => (X, Y, Z, W) = (value, value, value, value);

        public override bool Equals(object? obj) => obj is Vector4 vector && Equals(vector);
        public bool Equals(Vector4 other) => X == other.X && Y == other.Y && Z == other.Z &&W == other.W;
        public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);
        public override string ToString() => $"({X}, {Y}, {Z}, {W})";
        public static bool operator ==(Vector4 left, Vector4 right) => left.Equals(right);
        public static bool operator !=(Vector4 left, Vector4 right) => !(left == right);

        public static Vector4 operator +(Vector4 vec1, Vector4 vec2) => new Vector4(vec1.X + vec2.X, vec1.Y + vec2.Y, vec1.Z + vec2.Z, vec1.W + vec2.W);
        public static Vector4 operator +(Vector4 vec1, float right) => new Vector4(vec1.X + right, vec1.Y + right, vec1.Z + right, vec1.W + right);
        public static Vector4 operator -(Vector4 vec1, Vector4 vec2) => new Vector4(vec1.X - vec2.X, vec1.Y - vec2.Y, vec1.Z - vec2.Z, vec1.W - vec2.W);
        public static Vector4 operator -(Vector4 vec1, float right) => new Vector4(vec1.X - right, vec1.Y - right, vec1.Z - right, vec1.W - right);
        public static Vector4 operator *(Vector4 vec1, Vector4 vec2) => new Vector4(vec1.X * vec2.X, vec1.Y * vec2.Y, vec1.Z * vec2.Z, vec1.W * vec2.W);
        public static Vector4 operator *(Vector4 vec1, float right) => new Vector4(vec1.X * right, vec1.Y * right, vec1.Z * right, vec1.W * right);
        public static Vector4 operator /(Vector4 vec1, float right) => new Vector4(vec1.X / right, vec1.Y / right, vec1.Z / right, vec1.W / right);

        public static implicit operator TKVector4(Vector4 vec) => Unsafe.As<Vector4, TKVector4>(ref vec);
        public static implicit operator Vector4(TKVector4 vec) => Unsafe.As<TKVector4, Vector4>(ref vec);
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Color4 : IEquatable<Color4>
    {
        [FieldOffset(0)]
        public float R;
        [FieldOffset(4)]
        public float G;
        [FieldOffset(8)]
        public float B;
        [FieldOffset(12)]
        public float A;

        private static readonly float MaxValueAsFloat = byte.MaxValue;

        public Color4(float r, float g, float b) => (R, G, B, A) = (r, g, b, 0f);
        public Color4(float r, float g, float b, float a) => (R, G, B, A) = (r, g, b, a);

        public override bool Equals(object? obj) => obj is Color4 color && Equals(color);

        public bool Equals(Color4 other) => R == other.R && G == other.G && B == other.B && A == other.A;

        public override int GetHashCode() => HashCode.Combine(R, G, B, A);
        public override string ToString() => $"(R, G, B, A) = ({R}, {G}, {B}, {A}) = ({ToByte(R)}, {ToByte(G)}, {ToByte(B)}, {ToByte(A)})";
        public static bool operator ==(Color4 left, Color4 right) => left.Equals(right);

        public static bool operator !=(Color4 left, Color4 right) => !(left == right);

        public static implicit operator TKColor4(Color4 color) => Unsafe.As<Color4, TKColor4>(ref color);
        public static implicit operator Color4(TKColor4 color) => Unsafe.As<TKColor4, Color4>(ref color);

        private static byte ToByte(float value)
        {
            var tmp = value * byte.MaxValue;
            return (tmp < 0f) ? (byte)0 :
                   (tmp > (float)byte.MaxValue) ? byte.MaxValue : (byte)tmp;
        }
    }
}
