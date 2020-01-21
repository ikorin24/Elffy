#nullable enable
using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TKVector2 = OpenTK.Vector2;
using TKVector3 = OpenTK.Vector3;
using TKVector4 = OpenTK.Vector4;
using TKColor4 = OpenTK.Graphics.Color4;

namespace Elffy
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
        public static readonly Vector2 Zero = new Vector2(0, 0);
        public static readonly Vector2 One = new Vector2(1, 1);
        public static unsafe readonly int SizeInBytes = sizeof(Vector2);

        public Vector2 Yx => new Vector2(Y, X);

        public float LengthSquared => (X * X) + (Y * Y);
        public float Length => (float)Math.Sqrt(LengthSquared);

        public Vector2(float x, float y) => (X, Y) = (x, y);
        public Vector2(float value) => (X, Y) = (value, value);

        public Vector2 Dot(Vector2 vec) => this * vec;
        public static Vector2 Dot(Vector2 vec1, Vector2 vec2) => vec1 * vec2;
        public Vector2 Normalized() => ((TKVector2)this).Normalized();
        public override bool Equals(object? obj) => obj is Vector2 vector && Equals(vector);
        public bool Equals(Vector2 other) => X == other.X && Y == other.Y;
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public override string ToString() => $"({X}, {Y})";

        public static Vector2 operator -(Vector2 vec) => new Vector2(-vec.X, -vec.Y);
        public static bool operator ==(Vector2 left, Vector2 right) => left.Equals(right);
        public static bool operator !=(Vector2 left, Vector2 right) => !(left == right);
        public static Vector2 operator +(Vector2 vec1, Vector2 vec2) => new Vector2(vec1.X + vec2.X, vec1.Y + vec2.Y);
        public static Vector2 operator +(Vector2 vec1, float right) => new Vector2(vec1.X + right, vec1.Y + right);
        public static Vector2 operator -(Vector2 vec1, Vector2 vec2) => new Vector2(vec1.X - vec2.X, vec1.Y - vec2.Y);
        public static Vector2 operator -(Vector2 vec1, float right) => new Vector2(vec1.X - right, vec1.Y - right);
        public static Vector2 operator *(Vector2 vec1, Vector2 vec2) => new Vector2(vec1.X * vec2.X, vec1.Y * vec2.Y);
        public static Vector2 operator *(Vector2 vec1, float right) => new Vector2(vec1.X * right, vec1.Y * right);
        public static Vector2 operator *(float right, Vector2 vec1) => new Vector2(vec1.X * right, vec1.Y * right);
        public static Vector2 operator /(Vector2 vec1, float right) => new Vector2(vec1.X / right, vec1.Y / right);
        public static Vector2 operator /(float right, Vector2 vec1) => new Vector2(vec1.X / right, vec1.Y / right);
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
        public static readonly Vector3 Zero = new Vector3(0, 0, 0);
        public static readonly Vector3 One = new Vector3(1, 1, 1);
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
        public Vector3 Normalized() => ((TKVector3)this).Normalized();
        public override bool Equals(object? obj) => obj is Vector3 vector && Equals(vector);
        public bool Equals(Vector3 other) => X == other.X && Y == other.Y && Z == other.Z;
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
        public override string ToString() => $"({X}, {Y}, {Z})";

        public static Vector3 operator -(Vector3 vec) => new Vector3(-vec.X, -vec.Y, -vec.Z);
        public static bool operator ==(Vector3 left, Vector3 right) => left.Equals(right);
        public static bool operator !=(Vector3 left, Vector3 right) => !(left == right);
        public static Vector3 operator +(Vector3 vec1, Vector3 vec2) => new Vector3(vec1.X + vec2.X, vec1.Y + vec2.Y, vec1.Z + vec2.Z);
        public static Vector3 operator +(Vector3 vec1, float right) => new Vector3(vec1.X + right, vec1.Y + right, vec1.Z + right);
        public static Vector3 operator -(Vector3 vec1, Vector3 vec2) => new Vector3(vec1.X - vec2.X, vec1.Y - vec2.Y, vec1.Z - vec2.Z);
        public static Vector3 operator -(Vector3 vec1, float right) => new Vector3(vec1.X - right, vec1.Y - right, vec1.Z - right);
        public static Vector3 operator *(Vector3 vec1, Vector3 vec2) => new Vector3(vec1.X * vec2.X, vec1.Y * vec2.Y, vec1.Z * vec2.Z);
        public static Vector3 operator *(Vector3 vec1, float right) => new Vector3(vec1.X * right, vec1.Y * right, vec1.Z * right);
        public static Vector3 operator *(float right, Vector3 vec1) => new Vector3(vec1.X * right, vec1.Y * right, vec1.Z * right);
        public static Vector3 operator /(Vector3 vec1, float right) => new Vector3(vec1.X / right, vec1.Y / right, vec1.Z / right);
        public static Vector3 operator /(float right, Vector3 vec1) => new Vector3(vec1.X / right, vec1.Y / right, vec1.Z / right);

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
        public static readonly Vector4 Zero = new Vector4(0, 0, 0, 0);
        public static readonly Vector4 One = new Vector4(1, 1, 1, 1);
        public static unsafe readonly int SizeInBytes = sizeof(Vector4);

        // TODO: その他の組み合わせのプロパティ
        public Vector3 Xyz => new Vector3(X, Y, Z);

        public float LengthSquared => (X * X) + (Y * Y) + (Z * Z) + (W * W);
        public float Length => (float)Math.Sqrt(LengthSquared);
        public Vector4(float x, float y, float z, float w) => (X, Y, Z, W) = (x, y, z, w);
        public Vector4(Vector4 v) => this = v;
        public Vector4(Vector3 v) => (X, Y, Z, W) = (v.X, v.Y, v.Z, 0);
        public Vector4(Vector3 v, float w) => (X, Y, Z, W) = (v.X, v.Y, v.Z, w);
        public Vector4(Vector2 v) => (X, Y, Z, W) = (v.X, v.Y, 0, 0);
        public Vector4(Vector2 v, float z, float w) => (X, Y, Z, W) = (v.X, v.Y, z, w);
        public Vector4(float value) => (X, Y, Z, W) = (value, value, value, value);

        public Vector4 Dot(Vector4 vec) => this * vec;
        public static Vector4 Dot(Vector4 vec1, Vector4 vec2) => vec1 * vec2;
        public Vector4 Normalized() => ((TKVector4)this).Normalized();
        public override bool Equals(object? obj) => obj is Vector4 vector && Equals(vector);
        public bool Equals(Vector4 other) => X == other.X && Y == other.Y && Z == other.Z && W == other.W;
        public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);
        public override string ToString() => $"({X}, {Y}, {Z}, {W})";

        public static Vector4 operator -(Vector4 vec) => new Vector4(-vec.X, -vec.Y, -vec.Z, -vec.W);
        public static bool operator ==(Vector4 left, Vector4 right) => left.Equals(right);
        public static bool operator !=(Vector4 left, Vector4 right) => !(left == right);
        public static Vector4 operator +(Vector4 vec1, Vector4 vec2) => new Vector4(vec1.X + vec2.X, vec1.Y + vec2.Y, vec1.Z + vec2.Z, vec1.W + vec2.W);
        public static Vector4 operator +(Vector4 vec1, float right) => new Vector4(vec1.X + right, vec1.Y + right, vec1.Z + right, vec1.W + right);
        public static Vector4 operator -(Vector4 vec1, Vector4 vec2) => new Vector4(vec1.X - vec2.X, vec1.Y - vec2.Y, vec1.Z - vec2.Z, vec1.W - vec2.W);
        public static Vector4 operator -(Vector4 vec1, float right) => new Vector4(vec1.X - right, vec1.Y - right, vec1.Z - right, vec1.W - right);
        public static Vector4 operator *(Vector4 vec1, Vector4 vec2) => new Vector4(vec1.X * vec2.X, vec1.Y * vec2.Y, vec1.Z * vec2.Z, vec1.W * vec2.W);
        public static Vector4 operator *(Vector4 vec1, float right) => new Vector4(vec1.X * right, vec1.Y * right, vec1.Z * right, vec1.W * right);
        public static Vector4 operator *(float right, Vector4 vec1) => new Vector4(vec1.X * right, vec1.Y * right, vec1.Z * right, vec1.W * right);
        public static Vector4 operator /(Vector4 vec1, float right) => new Vector4(vec1.X / right, vec1.Y / right, vec1.Z / right, vec1.W / right);
        public static Vector4 operator /(float right, Vector4 vec1) => new Vector4(vec1.X / right, vec1.Y / right, vec1.Z / right, vec1.W / right);

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

        public Color4(float r, float g, float b) => (R, G, B, A) = (r, g, b, 1f);
        public Color4(float value) => (R, G, B, A) = (value, value, value, 1f);
        public Color4(float r, float g, float b, float a) => (R, G, B, A) = (r, g, b, a);

        public override bool Equals(object? obj) => obj is Color4 color && Equals(color);

        public bool Equals(Color4 other) => R == other.R && G == other.G && B == other.B && A == other.A;

        public override int GetHashCode() => HashCode.Combine(R, G, B, A);
        public override string ToString() => $"(R, G, B, A) = ({R}, {G}, {B}, {A}) = ({ToByte(R)}, {ToByte(G)}, {ToByte(B)}, {ToByte(A)})";
        public static bool operator ==(Color4 left, Color4 right) => left.Equals(right);

        public static bool operator !=(Color4 left, Color4 right) => !(left == right);

        public static implicit operator TKColor4(Color4 color) => Unsafe.As<Color4, TKColor4>(ref color);
        public static implicit operator Color4(TKColor4 color) => Unsafe.As<TKColor4, Color4>(ref color);
        public static explicit operator Color(Color4 color) => (Color)(TKColor4)color;
        public static implicit operator Color4(Color color) => (TKColor4)color;

        private static byte ToByte(float value)
        {
            var tmp = value * byte.MaxValue;
            return (tmp < 0f) ? (byte)0 :
                   (tmp > (float)byte.MaxValue) ? byte.MaxValue : (byte)tmp;
        }


        public static Color4 MediumAquamarine => TKColor4.MediumAquamarine;
        public static Color4 MediumBlue => TKColor4.MediumBlue;
        public static Color4 MediumOrchid => TKColor4.MediumOrchid;
        public static Color4 MediumPurple => TKColor4.MediumPurple;
        public static Color4 MediumSeaGreen => TKColor4.MediumSeaGreen;
        public static Color4 MediumSlateBlue => TKColor4.MediumSlateBlue;
        public static Color4 MediumSpringGreen => TKColor4.MediumSpringGreen;
        public static Color4 MediumTurquoise => TKColor4.MediumTurquoise;
        public static Color4 MintCream => TKColor4.MintCream;
        public static Color4 MidnightBlue => TKColor4.MidnightBlue;
        public static Color4 Maroon => TKColor4.Maroon;
        public static Color4 MistyRose => TKColor4.MistyRose;
        public static Color4 Moccasin => TKColor4.Moccasin;
        public static Color4 NavajoWhite => TKColor4.NavajoWhite;
        public static Color4 Navy => TKColor4.Navy;
        public static Color4 OldLace => TKColor4.OldLace;
        public static Color4 MediumVioletRed => TKColor4.MediumVioletRed;
        public static Color4 Magenta => TKColor4.Magenta;
        public static Color4 Lime => TKColor4.Lime;
        public static Color4 LimeGreen => TKColor4.LimeGreen;
        public static Color4 LavenderBlush => TKColor4.LavenderBlush;
        public static Color4 LawnGreen => TKColor4.LawnGreen;
        public static Color4 LemonChiffon => TKColor4.LemonChiffon;
        public static Color4 LightBlue => TKColor4.LightBlue;
        public static Color4 LightCoral => TKColor4.LightCoral;
        public static Color4 LightCyan => TKColor4.LightCyan;
        public static Color4 LightGoldenrodYellow => TKColor4.LightGoldenrodYellow;
        public static Color4 LightGreen => TKColor4.LightGreen;
        public static Color4 LightGray => TKColor4.LightGray;
        public static Color4 LightPink => TKColor4.LightPink;
        public static Color4 LightSalmon => TKColor4.LightSalmon;
        public static Color4 LightSeaGreen => TKColor4.LightSeaGreen;
        public static Color4 LightSkyBlue => TKColor4.LightSkyBlue;
        public static Color4 LightSlateGray => TKColor4.LightSlateGray;
        public static Color4 LightSteelBlue => TKColor4.LightSteelBlue;
        public static Color4 LightYellow => TKColor4.LightYellow;
        public static Color4 Olive => TKColor4.Olive;
        public static Color4 Linen => TKColor4.Linen;
        public static Color4 OliveDrab => TKColor4.OliveDrab;
        public static Color4 Orchid => TKColor4.Orchid;
        public static Color4 OrangeRed => TKColor4.OrangeRed;
        public static Color4 Silver => TKColor4.Silver;
        public static Color4 SkyBlue => TKColor4.SkyBlue;
        public static Color4 SlateBlue => TKColor4.SlateBlue;
        public static Color4 SlateGray => TKColor4.SlateGray;
        public static Color4 Snow => TKColor4.Snow;
        public static Color4 SpringGreen => TKColor4.SpringGreen;
        public static Color4 SteelBlue => TKColor4.SteelBlue;
        public static Color4 Sienna => TKColor4.Sienna;
        public static Color4 Tan => TKColor4.Tan;
        public static Color4 Thistle => TKColor4.Thistle;
        public static Color4 Tomato => TKColor4.Tomato;
        public static Color4 Turquoise => TKColor4.Turquoise;
        public static Color4 Violet => TKColor4.Violet;
        public static Color4 Wheat => TKColor4.Wheat;
        public static Color4 White => TKColor4.White;
        public static Color4 WhiteSmoke => TKColor4.WhiteSmoke;
        public static Color4 Teal => TKColor4.Teal;
        public static Color4 SeaShell => TKColor4.SeaShell;
        public static Color4 SeaGreen => TKColor4.SeaGreen;
        public static Color4 SandyBrown => TKColor4.SandyBrown;
        public static Color4 Lavender => TKColor4.Lavender;
        public static Color4 PaleGoldenrod => TKColor4.PaleGoldenrod;
        public static Color4 PaleGreen => TKColor4.PaleGreen;
        public static Color4 PaleTurquoise => TKColor4.PaleTurquoise;
        public static Color4 PaleVioletRed => TKColor4.PaleVioletRed;
        public static Color4 PapayaWhip => TKColor4.PapayaWhip;
        public static Color4 PeachPuff => TKColor4.PeachPuff;
        public static Color4 Peru => TKColor4.Peru;
        public static Color4 Pink => TKColor4.Pink;
        public static Color4 Plum => TKColor4.Plum;
        public static Color4 PowderBlue => TKColor4.PowderBlue;
        public static Color4 Purple => TKColor4.Purple;
        public static Color4 Red => TKColor4.Red;
        public static Color4 RosyBrown => TKColor4.RosyBrown;
        public static Color4 RoyalBlue => TKColor4.RoyalBlue;
        public static Color4 SaddleBrown => TKColor4.SaddleBrown;
        public static Color4 Salmon => TKColor4.Salmon;
        public static Color4 Orange => TKColor4.Orange;
        public static Color4 Khaki => TKColor4.Khaki;
        public static Color4 IndianRed => TKColor4.IndianRed;
        public static Color4 Indigo => TKColor4.Indigo;
        public static Color4 DarkGray => TKColor4.DarkGray;
        public static Color4 DarkGoldenrod => TKColor4.DarkGoldenrod;
        public static Color4 DarkCyan => TKColor4.DarkCyan;
        public static Color4 DarkBlue => TKColor4.DarkBlue;
        public static Color4 Cyan => TKColor4.Cyan;
        public static Color4 Crimson => TKColor4.Crimson;
        public static Color4 Cornsilk => TKColor4.Cornsilk;
        public static Color4 CornflowerBlue => TKColor4.CornflowerBlue;
        public static Color4 Ivory => TKColor4.Ivory;
        public static Color4 Chocolate => TKColor4.Chocolate;
        public static Color4 Chartreuse => TKColor4.Chartreuse;
        public static Color4 CadetBlue => TKColor4.CadetBlue;
        public static Color4 DarkGreen => TKColor4.DarkGreen;
        public static Color4 BurlyWood => TKColor4.BurlyWood;
        public static Color4 BlueViolet => TKColor4.BlueViolet;
        public static Color4 Blue => TKColor4.Blue;
        public static Color4 BlanchedAlmond => TKColor4.BlanchedAlmond;
        public static Color4 Black => TKColor4.Black;
        public static Color4 Bisque => TKColor4.Bisque;
        public static Color4 Beige => TKColor4.Beige;
        public static Color4 Azure => TKColor4.Azure;
        public static Color4 Aquamarine => TKColor4.Aquamarine;
        public static Color4 Aqua => TKColor4.Aqua;
        public static Color4 AntiqueWhite => TKColor4.AntiqueWhite;
        public static Color4 AliceBlue => TKColor4.AliceBlue;
        public static Color4 Transparent => TKColor4.Transparent;
        public static Color4 Brown => TKColor4.Brown;
        public static Color4 DarkKhaki => TKColor4.DarkKhaki;
        public static Color4 Coral => TKColor4.Coral;
        public static Color4 DarkOliveGreen => TKColor4.DarkOliveGreen;
        public static Color4 Yellow => TKColor4.Yellow;
        public static Color4 HotPink => TKColor4.HotPink;
        public static Color4 DarkMagenta => TKColor4.DarkMagenta;
        public static Color4 GreenYellow => TKColor4.GreenYellow;
        public static Color4 Green => TKColor4.Green;
        public static Color4 Gray => TKColor4.Gray;
        public static Color4 Goldenrod => TKColor4.Goldenrod;
        public static Color4 Gold => TKColor4.Gold;
        public static Color4 GhostWhite => TKColor4.GhostWhite;
        public static Color4 Gainsboro => TKColor4.Gainsboro;
        public static Color4 Fuchsia => TKColor4.Fuchsia;
        public static Color4 ForestGreen => TKColor4.ForestGreen;
        public static Color4 FloralWhite => TKColor4.FloralWhite;
        public static Color4 Honeydew => TKColor4.Honeydew;
        public static Color4 DodgerBlue => TKColor4.DodgerBlue;
        public static Color4 Firebrick => TKColor4.Firebrick;
        public static Color4 DarkOrange => TKColor4.DarkOrange;
        public static Color4 DarkOrchid => TKColor4.DarkOrchid;
        public static Color4 DarkRed => TKColor4.DarkRed;
        public static Color4 DarkSalmon => TKColor4.DarkSalmon;
        public static Color4 DarkSeaGreen => TKColor4.DarkSeaGreen;
        public static Color4 YellowGreen => TKColor4.YellowGreen;
        public static Color4 DarkSlateGray => TKColor4.DarkSlateGray;
        public static Color4 DarkTurquoise => TKColor4.DarkTurquoise;
        public static Color4 DarkViolet => TKColor4.DarkViolet;
        public static Color4 DeepPink => TKColor4.DeepPink;
        public static Color4 DeepSkyBlue => TKColor4.DeepSkyBlue;
        public static Color4 DarkSlateBlue => TKColor4.DarkSlateBlue;
        public static Color4 DimGray => TKColor4.DimGray;
    }
}
