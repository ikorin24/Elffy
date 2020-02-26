#nullable enable
using System;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TKVector2 = OpenTK.Vector2;
using TKVector3 = OpenTK.Vector3;
using TKVector4 = OpenTK.Vector4;
using TKColor4 = OpenTK.Graphics.Color4;
using Elffy.Mathmatics;

namespace Elffy
{
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct Vector2 : IEquatable<Vector2>
    {
        [FieldOffset(0)]
        public readonly float X;
        [FieldOffset(4)]
        public readonly float Y;

        public static readonly Vector2 UnitX = new Vector2(1, 0);
        public static readonly Vector2 UnitY = new Vector2(0, 1);
        public static readonly Vector2 Zero = new Vector2(0, 0);
        public static readonly Vector2 One = new Vector2(1, 1);
        public static unsafe readonly int SizeInBytes = sizeof(Vector2);

        public readonly Vector2 Yx => new Vector2(Y, X);

        public readonly float LengthSquared => (X * X) + (Y * Y);
        public readonly float Length => (float)Math.Sqrt(LengthSquared);

        public Vector2(float x, float y) => (X, Y) = (x, y);
        public Vector2(float value) => (X, Y) = (value, value);

        public readonly Vector2 Dot(in Vector2 vec) => this * vec;
        public static Vector2 Dot(in Vector2 vec1, in Vector2 vec2) => vec1 * vec2;
        public readonly Vector2 Normalized() => ((TKVector2)this).Normalized();
        public readonly override bool Equals(object? obj) => obj is Vector2 vector && Equals(vector);
        public readonly bool Equals(Vector2 other) => X == other.X && Y == other.Y;
        public readonly override int GetHashCode() => HashCode.Combine(X, Y);
        public readonly override string ToString() => $"({X}, {Y})";

        public static Vector2 operator -(in Vector2 vec) => new Vector2(-vec.X, -vec.Y);
        public static bool operator ==(in Vector2 left, in Vector2 right) => left.Equals(right);
        public static bool operator !=(in Vector2 left, in Vector2 right) => !(left == right);
        public static Vector2 operator +(in Vector2 vec1, in Vector2 vec2) => new Vector2(vec1.X + vec2.X, vec1.Y + vec2.Y);
        public static Vector2 operator +(in Vector2 vec1, float right) => new Vector2(vec1.X + right, vec1.Y + right);
        public static Vector2 operator -(in Vector2 vec1, in Vector2 vec2) => new Vector2(vec1.X - vec2.X, vec1.Y - vec2.Y);
        public static Vector2 operator -(in Vector2 vec1, float right) => new Vector2(vec1.X - right, vec1.Y - right);
        public static Vector2 operator *(in Vector2 vec1, in Vector2 vec2) => new Vector2(vec1.X * vec2.X, vec1.Y * vec2.Y);
        public static Vector2 operator *(in Vector2 vec1, float right) => new Vector2(vec1.X * right, vec1.Y * right);
        public static Vector2 operator *(float right, in Vector2 vec1) => new Vector2(vec1.X * right, vec1.Y * right);
        public static Vector2 operator /(in Vector2 vec1, float right) => new Vector2(vec1.X / right, vec1.Y / right);
        public static Vector2 operator /(float right, in Vector2 vec1) => new Vector2(vec1.X / right, vec1.Y / right);
        public static implicit operator TKVector2(Vector2 vec) => Unsafe.As<Vector2, TKVector2>(ref vec);
        public static implicit operator Vector2(TKVector2 vec) => Unsafe.As<TKVector2, Vector2>(ref vec);
        public static implicit operator Vector2(in Point point) => new Vector2(point.X, point.Y);
        public static explicit operator Point(in Vector2 vec) => new Point((int)vec.X, (int)vec.Y);
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

        public readonly Vector2 Xy => new Vector2(X, Y);
        public readonly Vector2 Xz => new Vector2(X, Z);
        public readonly Vector2 Yx => new Vector2(Y, X);
        public readonly Vector2 Yz => new Vector2(Y, Z);
        public readonly Vector2 Zx => new Vector2(Z, X);
        public readonly Vector2 Zy => new Vector2(Z, Y);
        public readonly Vector3 Xzy => new Vector3(X, Z, Y);
        public readonly Vector3 Yxz => new Vector3(Y, X, Z);
        public readonly Vector3 Yzx => new Vector3(Y, Z, X);
        public readonly Vector3 Zxy => new Vector3(Z, X, Y);
        public readonly Vector3 Zyx => new Vector3(Z, Y, X);

        public readonly float LengthSquared => (X * X) + (Y * Y) + (Z * Z);
        public readonly float Length => (float)Math.Sqrt(LengthSquared);

        public Vector3(float x, float y, float z) => (X, Y, Z) = (x, y, z);
        public Vector3(Vector3 v) => this = v;
        public Vector3(Vector2 v) => (X, Y, Z) = (v.X, v.Y, 0);
        public Vector3(Vector2 v, float z) => (X, Y, Z) = (v.X, v.Y, z);
        public Vector3(in Vector4 v) => (X, Y, Z) = (v.X, v.Y, v.Z);
        public Vector3(float value) => (X, Y, Z) = (value, value, value);

        public readonly Vector3 Dot(in Vector3 vec) => this * vec;
        public static Vector3 Dot(in Vector3 vec1, in Vector3 vec2) => vec1 * vec2;
        public readonly Vector3 Cross(in Vector3 vec) => Cross(this, vec);
        public static Vector3 Cross(in Vector3 vec1, in Vector3 vec2) => new Vector3(vec1.Y * vec2.Z - vec1.Z * vec2.Y,
                                                                               vec1.Z * vec2.X - vec1.X * vec2.Z,
                                                                               vec1.X * vec2.Y - vec1.Y * vec2.X);
        public readonly Vector3 Normalized() => ((TKVector3)this).Normalized();
        public readonly override bool Equals(object? obj) => obj is Vector3 vector && Equals(vector);
        public readonly bool Equals(Vector3 other) => X == other.X && Y == other.Y && Z == other.Z;
        public readonly override int GetHashCode() => HashCode.Combine(X, Y, Z);
        public readonly override string ToString() => $"({X}, {Y}, {Z})";

        public static Vector3 operator -(in Vector3 vec) => new Vector3(-vec.X, -vec.Y, -vec.Z);
        public static bool operator ==(in Vector3 left, in Vector3 right) => left.Equals(right);
        public static bool operator !=(in Vector3 left, in Vector3 right) => !(left == right);
        public static Vector3 operator +(in Vector3 vec1, in Vector3 vec2) => new Vector3(vec1.X + vec2.X, vec1.Y + vec2.Y, vec1.Z + vec2.Z);
        public static Vector3 operator +(in Vector3 vec1, float right) => new Vector3(vec1.X + right, vec1.Y + right, vec1.Z + right);
        public static Vector3 operator -(in Vector3 vec1, in Vector3 vec2) => new Vector3(vec1.X - vec2.X, vec1.Y - vec2.Y, vec1.Z - vec2.Z);
        public static Vector3 operator -(in Vector3 vec1, float right) => new Vector3(vec1.X - right, vec1.Y - right, vec1.Z - right);
        public static Vector3 operator *(in Vector3 vec1, in Vector3 vec2) => new Vector3(vec1.X * vec2.X, vec1.Y * vec2.Y, vec1.Z * vec2.Z);
        public static Vector3 operator *(in Vector3 vec1, float right) => new Vector3(vec1.X * right, vec1.Y * right, vec1.Z * right);
        public static Vector3 operator *(float right, in Vector3 vec1) => new Vector3(vec1.X * right, vec1.Y * right, vec1.Z * right);
        public static Vector3 operator /(in Vector3 vec1, float right) => new Vector3(vec1.X / right, vec1.Y / right, vec1.Z / right);
        public static Vector3 operator /(float right, in Vector3 vec1) => new Vector3(vec1.X / right, vec1.Y / right, vec1.Z / right);

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
        public readonly Vector3 Xyz => new Vector3(X, Y, Z);

        public readonly float LengthSquared => (X * X) + (Y * Y) + (Z * Z) + (W * W);
        public readonly float Length => (float)Math.Sqrt(LengthSquared);
        public Vector4(float x, float y, float z, float w) => (X, Y, Z, W) = (x, y, z, w);
        public Vector4(Vector4 v) => this = v;
        public Vector4(in Vector3 v) => (X, Y, Z, W) = (v.X, v.Y, v.Z, 0);
        public Vector4(in Vector3 v, float w) => (X, Y, Z, W) = (v.X, v.Y, v.Z, w);
        public Vector4(in Vector2 v) => (X, Y, Z, W) = (v.X, v.Y, 0, 0);
        public Vector4(in Vector2 v, float z, float w) => (X, Y, Z, W) = (v.X, v.Y, z, w);
        public Vector4(float value) => (X, Y, Z, W) = (value, value, value, value);

        public readonly Vector4 Dot(in Vector4 vec) => this * vec;
        public static Vector4 Dot(in Vector4 vec1, in Vector4 vec2) => vec1 * vec2;
        public readonly Vector4 Normalized() => ((TKVector4)this).Normalized();
        public readonly override bool Equals(object? obj) => obj is Vector4 vector && Equals(vector);
        public readonly bool Equals(Vector4 other) => X == other.X && Y == other.Y && Z == other.Z && W == other.W;
        public readonly override int GetHashCode() => HashCode.Combine(X, Y, Z, W);
        public readonly override string ToString() => $"({X}, {Y}, {Z}, {W})";

        public static Vector4 operator -(in Vector4 vec) => new Vector4(-vec.X, -vec.Y, -vec.Z, -vec.W);
        public static bool operator ==(in Vector4 left, in Vector4 right) => left.Equals(right);
        public static bool operator !=(in Vector4 left, in Vector4 right) => !(left == right);
        public static Vector4 operator +(in Vector4 vec1, in Vector4 vec2) => new Vector4(vec1.X + vec2.X, vec1.Y + vec2.Y, vec1.Z + vec2.Z, vec1.W + vec2.W);
        public static Vector4 operator +(in Vector4 vec1, float right) => new Vector4(vec1.X + right, vec1.Y + right, vec1.Z + right, vec1.W + right);
        public static Vector4 operator -(in Vector4 vec1, in Vector4 vec2) => new Vector4(vec1.X - vec2.X, vec1.Y - vec2.Y, vec1.Z - vec2.Z, vec1.W - vec2.W);
        public static Vector4 operator -(in Vector4 vec1, float right) => new Vector4(vec1.X - right, vec1.Y - right, vec1.Z - right, vec1.W - right);
        public static Vector4 operator *(in Vector4 vec1, in Vector4 vec2) => new Vector4(vec1.X * vec2.X, vec1.Y * vec2.Y, vec1.Z * vec2.Z, vec1.W * vec2.W);
        public static Vector4 operator *(in Vector4 vec1, float right) => new Vector4(vec1.X * right, vec1.Y * right, vec1.Z * right, vec1.W * right);
        public static Vector4 operator *(float right, in Vector4 vec1) => new Vector4(vec1.X * right, vec1.Y * right, vec1.Z * right, vec1.W * right);
        public static Vector4 operator /(in Vector4 vec1, float right) => new Vector4(vec1.X / right, vec1.Y / right, vec1.Z / right, vec1.W / right);
        public static Vector4 operator /(float right, in Vector4 vec1) => new Vector4(vec1.X / right, vec1.Y / right, vec1.Z / right, vec1.W / right);

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

        public readonly override bool Equals(object? obj) => obj is Color4 color && Equals(color);

        public readonly bool Equals(Color4 other) => R == other.R && G == other.G && B == other.B && A == other.A;

        public readonly override int GetHashCode() => HashCode.Combine(R, G, B, A);
        public readonly override string ToString() => $"(R, G, B, A) = ({R}, {G}, {B}, {A}) = ({ToByte(R)}, {ToByte(G)}, {ToByte(B)}, {ToByte(A)})";
        public static bool operator ==(in Color4 left, in Color4 right) => left.Equals(right);

        public static bool operator !=(in Color4 left, in Color4 right) => !(left == right);

        public static implicit operator TKColor4(Color4 color) => Unsafe.As<Color4, TKColor4>(ref color);
        public static implicit operator Color4(TKColor4 color) => Unsafe.As<TKColor4, Color4>(ref color);
        public static explicit operator Color(in Color4 color) => (Color)(TKColor4)color;
        public static implicit operator Color4(in Color color) => (TKColor4)color;

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

    [StructLayout(LayoutKind.Explicit)]
    public struct Matrix2 : IEquatable<Matrix2>
    {
        [FieldOffset(0)]
        public float M00;
        [FieldOffset(4)]
        public float M01;
        [FieldOffset(8)]
        public float M10;
        [FieldOffset(12)]
        public float M11;

        public Matrix2(float m00, float m01, float m10, float m11)
        {
            M00 = m00;
            M01 = m01;
            M10 = m10;
            M11 = m11;
        }

        public void Transpose()
        {
            var tmp = M01;
            M01 = M10;
            M10 = tmp;
        }

        public readonly Matrix2 Transposed()
        {
            return new Matrix2(M00, M10, M01, M11);
        }

        public static Matrix2 Rotate(float theta)
        {
            var cos = MathTool.Cos(theta);
            var sin = MathTool.Sin(theta);
            return new Matrix2(cos, sin,
                               -sin, cos);
        }

        public override bool Equals(object? obj)
        {
            return obj is Matrix2 matrix && Equals(matrix);
        }

        public bool Equals(Matrix2 other)
        {
            return M00 == other.M00 &&
                   M01 == other.M01 &&
                   M10 == other.M10 &&
                   M11 == other.M11;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(M00, M01, M10, M11);
        }

        public static Vector2 operator *(in Vector2 vec, in Matrix2 matrix)
        {
            return new Vector2(matrix.M00 * vec.X + matrix.M10 * vec.Y,
                               matrix.M01 * vec.X + matrix.M11 * vec.Y);
        }

        public static bool operator ==(Matrix2 left, Matrix2 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Matrix2 left, Matrix2 right)
        {
            return !(left == right);
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Matrix3 : IEquatable<Matrix3>
    {
        [FieldOffset(0)]
        public float M00;
        [FieldOffset(4)]
        public float M01;
        [FieldOffset(8)]
        public float M02;
        [FieldOffset(12)]
        public float M10;
        [FieldOffset(16)]
        public float M11;
        [FieldOffset(20)]
        public float M12;
        [FieldOffset(24)]
        public float M20;
        [FieldOffset(28)]
        public float M21;
        [FieldOffset(32)]
        public float M22;

        public Matrix3(float m00, float m01, float m02,
                       float m10, float m11, float m12,
                       float m20, float m21, float m22)
        {
            M00 = m00;
            M01 = m01;
            M02 = m02;
            M10 = m10;
            M11 = m11;
            M12 = m12;
            M20 = m20;
            M21 = m21;
            M22 = m22;
        }

        public override bool Equals(object? obj)
        {
            return obj is Matrix3 matrix && Equals(matrix);
        }

        public bool Equals(Matrix3 other)
        {
            return M00 == other.M00 &&
                   M01 == other.M01 &&
                   M02 == other.M02 &&
                   M10 == other.M10 &&
                   M11 == other.M11 &&
                   M12 == other.M12 &&
                   M20 == other.M20 &&
                   M21 == other.M21 &&
                   M22 == other.M22;
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(M00);
            hash.Add(M01);
            hash.Add(M02);
            hash.Add(M10);
            hash.Add(M11);
            hash.Add(M12);
            hash.Add(M20);
            hash.Add(M21);
            hash.Add(M22);
            return hash.ToHashCode();
        }

        public static Vector3 operator *(in Vector3 vec, in Matrix3 matrix)
        {
            return new Vector3(matrix.M00 * vec.X + matrix.M10 * vec.Y + matrix.M20 * vec.Z,
                               matrix.M01 * vec.X + matrix.M11 * vec.Y + matrix.M21 * vec.Z,
                               matrix.M02 * vec.X + matrix.M12 * vec.Y + matrix.M22 * vec.Z);
        }

        public static Matrix3 operator *(in Matrix3 m1, in Matrix3 m2)
        {
            return new Matrix3(m1.M00 * m2.M00 + m1.M01 * m2.M10 + m1.M02 * m2.M20,   m1.M00 * m2.M01 + m1.M01 * m2.M11 + m1.M02 * m2.M21,   m1.M00 * m2.M02 + m1.M01 * m2.M12 + m1.M02 * m2.M22,

                               m1.M10 * m2.M00 + m1.M11 * m2.M10 + m1.M12 * m2.M20,   m1.M10 * m2.M01 + m1.M11 * m2.M11 + m1.M12 * m2.M21,   m1.M10 * m2.M02 + m1.M11 * m2.M12 + m1.M12 * m2.M22,

                               m1.M20 * m2.M00 + m1.M21 * m2.M10 + m1.M22 * m2.M20,   m1.M20 * m2.M01 + m1.M21 * m2.M11 + m1.M22 * m2.M21,   m1.M20 * m2.M02 + m1.M21 * m2.M12 + m1.M22 * m2.M22);
        }

        public static bool operator ==(Matrix3 left, Matrix3 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Matrix3 left, Matrix3 right)
        {
            return !(left == right);
        }
    }
}
