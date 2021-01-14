#nullable enable
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Numerics;
using Cysharp.Text;
using Elffy.Effective.Unsafes;
using NVec4 = System.Numerics.Vector4;

namespace Elffy
{
    [StructLayout(LayoutKind.Explicit)]
    public struct ColorByte : IEquatable<ColorByte>
    {
        [FieldOffset(0)]
        public byte R;
        [FieldOffset(1)]
        public byte G;
        [FieldOffset(2)]
        public byte B;
        [FieldOffset(3)]
        public byte A;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ColorByte(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out byte r, out byte g, out byte b, out byte a) => (r, g, b, a) = (R, G, B, A);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Color4 ToColor4() => new Color4(R / byte.MaxValue, G / byte.MaxValue, B / byte.MaxValue, A / byte.MaxValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ColorByte PreMultiplied()
        {
            const float f = 1f / byte.MaxValue;
            float af = A * f;

            var c = new NVec4(R, G, B, A) * new NVec4(af, af, af, 1f);
            return new ColorByte((byte)c.X, (byte)c.Y, (byte)c.Z, (byte)c.W);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PreMultiply()
        {
            const float f = 1f / byte.MaxValue;
            float af = A * f;

            var c = new NVec4(R, G, B, A) * new NVec4(af, af, af, 1f);

            R = (byte)c.X;
            G = (byte)c.Y;
            B = (byte)c.Z;
            A = (byte)c.W;
        }

        public readonly override bool Equals(object? obj) => obj is ColorByte color && Equals(color);

        public readonly bool Equals(ColorByte other) => R == other.R && G == other.G && B == other.B && A == other.A;

        public readonly override int GetHashCode() => HashCode.Combine(R, G, B, A);

        public static bool operator ==(in ColorByte left, in ColorByte right) => left.Equals(right);

        public static bool operator !=(in ColorByte left, in ColorByte right) => !(left == right);

        public readonly override string ToString() => ZString.Concat("(R: ", R, ", G: ", G, ", B: ", B, ", A: ", A, ")");

        public static explicit operator Color4(in ColorByte color) => color.ToColor4();

        /// <summary>Gets the system color with (R, G, B, A) = (102, 205, 170, 255).</summary>
        public static ColorByte MediumAquamarine => new ColorByte(102, 205, 170, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 0, 205, 255).</summary>
        public static ColorByte MediumBlue => new ColorByte(0, 0, 205, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (186, 85, 211, 255).</summary>
        public static ColorByte MediumOrchid => new ColorByte(186, 85, 211, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (147, 112, 219, 255).</summary>
        public static ColorByte MediumPurple => new ColorByte(147, 112, 219, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (60, 179, 113, 255).</summary>
        public static ColorByte MediumSeaGreen => new ColorByte(60, 179, 113, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (123, 104, 238, 255).</summary>
        public static ColorByte MediumSlateBlue => new ColorByte(123, 104, 238, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 250, 154, 255).</summary>
        public static ColorByte MediumSpringGreen => new ColorByte(0, 250, 154, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (72, 209, 204, 255).</summary>
        public static ColorByte MediumTurquoise => new ColorByte(72, 209, 204, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (245, 255, 250, 255).</summary>
        public static ColorByte MintCream => new ColorByte(245, 255, 250, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (25, 25, 112, 255).</summary>
        public static ColorByte MidnightBlue => new ColorByte(25, 25, 112, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (128, 0, 0, 255).</summary>
        public static ColorByte Maroon => new ColorByte(128, 0, 0, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 228, 225, 255).</summary>
        public static ColorByte MistyRose => new ColorByte(255, 228, 225, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 228, 181, 255).</summary>
        public static ColorByte Moccasin => new ColorByte(255, 228, 181, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 222, 173, 255).</summary>
        public static ColorByte NavajoWhite => new ColorByte(255, 222, 173, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 0, 128, 255).</summary>
        public static ColorByte Navy => new ColorByte(0, 0, 128, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (253, 245, 230, 255).</summary>
        public static ColorByte OldLace => new ColorByte(253, 245, 230, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (199, 21, 133, 255).</summary>
        public static ColorByte MediumVioletRed => new ColorByte(199, 21, 133, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 0, 255, 255).</summary>
        public static ColorByte Magenta => new ColorByte(255, 0, 255, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 255, 0, 255).</summary>
        public static ColorByte Lime => new ColorByte(0, 255, 0, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (50, 205, 50, 255).</summary>
        public static ColorByte LimeGreen => new ColorByte(50, 205, 50, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 240, 245, 255).</summary>
        public static ColorByte LavenderBlush => new ColorByte(255, 240, 245, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (124, 252, 0, 255).</summary>
        public static ColorByte LawnGreen => new ColorByte(124, 252, 0, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 250, 205, 255).</summary>
        public static ColorByte LemonChiffon => new ColorByte(255, 250, 205, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (173, 216, 230, 255).</summary>
        public static ColorByte LightBlue => new ColorByte(173, 216, 230, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (240, 128, 128, 255).</summary>
        public static ColorByte LightCoral => new ColorByte(240, 128, 128, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (224, 255, 255, 255).</summary>
        public static ColorByte LightCyan => new ColorByte(224, 255, 255, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (250, 250, 210, 255).</summary>
        public static ColorByte LightGoldenrodYellow => new ColorByte(250, 250, 210, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (144, 238, 144, 255).</summary>
        public static ColorByte LightGreen => new ColorByte(144, 238, 144, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (211, 211, 211, 255).</summary>
        public static ColorByte LightGray => new ColorByte(211, 211, 211, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 182, 193, 255).</summary>
        public static ColorByte LightPink => new ColorByte(255, 182, 193, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 160, 122, 255).</summary>
        public static ColorByte LightSalmon => new ColorByte(255, 160, 122, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (32, 178, 170, 255).</summary>
        public static ColorByte LightSeaGreen => new ColorByte(32, 178, 170, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (135, 206, 250, 255).</summary>
        public static ColorByte LightSkyBlue => new ColorByte(135, 206, 250, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (119, 136, 153, 255).</summary>
        public static ColorByte LightSlateGray => new ColorByte(119, 136, 153, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (176, 196, 222, 255).</summary>
        public static ColorByte LightSteelBlue => new ColorByte(176, 196, 222, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 255, 224, 255).</summary>
        public static ColorByte LightYellow => new ColorByte(255, 255, 224, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (128, 128, 0, 255).</summary>
        public static ColorByte Olive => new ColorByte(128, 128, 0, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (250, 240, 230, 255).</summary>
        public static ColorByte Linen => new ColorByte(250, 240, 230, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (107, 142, 35, 255).</summary>
        public static ColorByte OliveDrab => new ColorByte(107, 142, 35, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (218, 112, 214, 255).</summary>
        public static ColorByte Orchid => new ColorByte(218, 112, 214, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 69, 0, 255).</summary>
        public static ColorByte OrangeRed => new ColorByte(255, 69, 0, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (192, 192, 192, 255).</summary>
        public static ColorByte Silver => new ColorByte(192, 192, 192, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (135, 206, 235, 255).</summary>
        public static ColorByte SkyBlue => new ColorByte(135, 206, 235, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (106, 90, 205, 255).</summary>
        public static ColorByte SlateBlue => new ColorByte(106, 90, 205, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (112, 128, 144, 255).</summary>
        public static ColorByte SlateGray => new ColorByte(112, 128, 144, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 250, 250, 255).</summary>
        public static ColorByte Snow => new ColorByte(255, 250, 250, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 255, 127, 255).</summary>
        public static ColorByte SpringGreen => new ColorByte(0, 255, 127, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (70, 130, 180, 255).</summary>
        public static ColorByte SteelBlue => new ColorByte(70, 130, 180, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (160, 82, 45, 255).</summary>
        public static ColorByte Sienna => new ColorByte(160, 82, 45, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (210, 180, 140, 255).</summary>
        public static ColorByte Tan => new ColorByte(210, 180, 140, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (216, 191, 216, 255).</summary>
        public static ColorByte Thistle => new ColorByte(216, 191, 216, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 99, 71, 255).</summary>
        public static ColorByte Tomato => new ColorByte(255, 99, 71, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (64, 224, 208, 255).</summary>
        public static ColorByte Turquoise => new ColorByte(64, 224, 208, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (238, 130, 238, 255).</summary>
        public static ColorByte Violet => new ColorByte(238, 130, 238, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (245, 222, 179, 255).</summary>
        public static ColorByte Wheat => new ColorByte(245, 222, 179, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 255, 255, 255).</summary>
        public static ColorByte White => new ColorByte(255, 255, 255, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (245, 245, 245, 255).</summary>
        public static ColorByte WhiteSmoke => new ColorByte(245, 245, 245, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 128, 128, 255).</summary>
        public static ColorByte Teal => new ColorByte(0, 128, 128, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 245, 238, 255).</summary>
        public static ColorByte SeaShell => new ColorByte(255, 245, 238, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (46, 139, 87, 255).</summary>
        public static ColorByte SeaGreen => new ColorByte(46, 139, 87, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (244, 164, 96, 255).</summary>
        public static ColorByte SandyBrown => new ColorByte(244, 164, 96, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (230, 230, 250, 255).</summary>
        public static ColorByte Lavender => new ColorByte(230, 230, 250, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (238, 232, 170, 255).</summary>
        public static ColorByte PaleGoldenrod => new ColorByte(238, 232, 170, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (152, 251, 152, 255).</summary>
        public static ColorByte PaleGreen => new ColorByte(152, 251, 152, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (175, 238, 238, 255).</summary>
        public static ColorByte PaleTurquoise => new ColorByte(175, 238, 238, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (219, 112, 147, 255).</summary>
        public static ColorByte PaleVioletRed => new ColorByte(219, 112, 147, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 239, 213, 255).</summary>
        public static ColorByte PapayaWhip => new ColorByte(255, 239, 213, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 218, 185, 255).</summary>
        public static ColorByte PeachPuff => new ColorByte(255, 218, 185, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (205, 133, 63, 255).</summary>
        public static ColorByte Peru => new ColorByte(205, 133, 63, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 192, 203, 255).</summary>
        public static ColorByte Pink => new ColorByte(255, 192, 203, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (221, 160, 221, 255).</summary>
        public static ColorByte Plum => new ColorByte(221, 160, 221, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (176, 224, 230, 255).</summary>
        public static ColorByte PowderBlue => new ColorByte(176, 224, 230, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (128, 0, 128, 255).</summary>
        public static ColorByte Purple => new ColorByte(128, 0, 128, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 0, 0, 255).</summary>
        public static ColorByte Red => new ColorByte(255, 0, 0, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (188, 143, 143, 255).</summary>
        public static ColorByte RosyBrown => new ColorByte(188, 143, 143, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (65, 105, 225, 255).</summary>
        public static ColorByte RoyalBlue => new ColorByte(65, 105, 225, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (139, 69, 19, 255).</summary>
        public static ColorByte SaddleBrown => new ColorByte(139, 69, 19, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (250, 128, 114, 255).</summary>
        public static ColorByte Salmon => new ColorByte(250, 128, 114, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 165, 0, 255).</summary>
        public static ColorByte Orange => new ColorByte(255, 165, 0, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (240, 230, 140, 255).</summary>
        public static ColorByte Khaki => new ColorByte(240, 230, 140, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (205, 92, 92, 255).</summary>
        public static ColorByte IndianRed => new ColorByte(205, 92, 92, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (75, 0, 130, 255).</summary>
        public static ColorByte Indigo => new ColorByte(75, 0, 130, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (169, 169, 169, 255).</summary>
        public static ColorByte DarkGray => new ColorByte(169, 169, 169, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (184, 134, 11, 255).</summary>
        public static ColorByte DarkGoldenrod => new ColorByte(184, 134, 11, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 139, 139, 255).</summary>
        public static ColorByte DarkCyan => new ColorByte(0, 139, 139, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 0, 139, 255).</summary>
        public static ColorByte DarkBlue => new ColorByte(0, 0, 139, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 255, 255, 255).</summary>
        public static ColorByte Cyan => new ColorByte(0, 255, 255, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (220, 20, 60, 255).</summary>
        public static ColorByte Crimson => new ColorByte(220, 20, 60, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 248, 220, 255).</summary>
        public static ColorByte Cornsilk => new ColorByte(255, 248, 220, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (100, 149, 237, 255).</summary>
        public static ColorByte CornflowerBlue => new ColorByte(100, 149, 237, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 255, 240, 255).</summary>
        public static ColorByte Ivory => new ColorByte(255, 255, 240, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (210, 105, 30, 255).</summary>
        public static ColorByte Chocolate => new ColorByte(210, 105, 30, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (127, 255, 0, 255).</summary>
        public static ColorByte Chartreuse => new ColorByte(127, 255, 0, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (95, 158, 160, 255).</summary>
        public static ColorByte CadetBlue => new ColorByte(95, 158, 160, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 100, 0, 255).</summary>
        public static ColorByte DarkGreen => new ColorByte(0, 100, 0, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (222, 184, 135, 255).</summary>
        public static ColorByte BurlyWood => new ColorByte(222, 184, 135, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (138, 43, 226, 255).</summary>
        public static ColorByte BlueViolet => new ColorByte(138, 43, 226, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 0, 255, 255).</summary>
        public static ColorByte Blue => new ColorByte(0, 0, 255, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 235, 205, 255).</summary>
        public static ColorByte BlanchedAlmond => new ColorByte(255, 235, 205, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 0, 0, 255).</summary>
        public static ColorByte Black => new ColorByte(0, 0, 0, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 228, 196, 255).</summary>
        public static ColorByte Bisque => new ColorByte(255, 228, 196, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (245, 245, 220, 255).</summary>
        public static ColorByte Beige => new ColorByte(245, 245, 220, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (240, 255, 255, 255).</summary>
        public static ColorByte Azure => new ColorByte(240, 255, 255, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (127, 255, 212, 255).</summary>
        public static ColorByte Aquamarine => new ColorByte(127, 255, 212, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 255, 255, 255).</summary>
        public static ColorByte Aqua => new ColorByte(0, 255, 255, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (250, 235, 215, 255).</summary>
        public static ColorByte AntiqueWhite => new ColorByte(250, 235, 215, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (240, 248, 255, 255).</summary>
        public static ColorByte AliceBlue => new ColorByte(240, 248, 255, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 255, 255, 0).</summary>
        public static ColorByte Transparent => new ColorByte(255, 255, 255, 0);
        /// <summary>Gets the system color with (R, G, B, A) = (165, 42, 42, 255).</summary>
        public static ColorByte Brown => new ColorByte(165, 42, 42, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (189, 183, 107, 255).</summary>
        public static ColorByte DarkKhaki => new ColorByte(189, 183, 107, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 127, 80, 255).</summary>
        public static ColorByte Coral => new ColorByte(255, 127, 80, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (85, 107, 47, 255).</summary>
        public static ColorByte DarkOliveGreen => new ColorByte(85, 107, 47, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 255, 0, 255).</summary>
        public static ColorByte Yellow => new ColorByte(255, 255, 0, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 105, 180, 255).</summary>
        public static ColorByte HotPink => new ColorByte(255, 105, 180, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (139, 0, 139, 255).</summary>
        public static ColorByte DarkMagenta => new ColorByte(139, 0, 139, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (173, 255, 47, 255).</summary>
        public static ColorByte GreenYellow => new ColorByte(173, 255, 47, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 128, 0, 255).</summary>
        public static ColorByte Green => new ColorByte(0, 128, 0, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (128, 128, 128, 255).</summary>
        public static ColorByte Gray => new ColorByte(128, 128, 128, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (218, 165, 32, 255).</summary>
        public static ColorByte Goldenrod => new ColorByte(218, 165, 32, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 215, 0, 255).</summary>
        public static ColorByte Gold => new ColorByte(255, 215, 0, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (248, 248, 255, 255).</summary>
        public static ColorByte GhostWhite => new ColorByte(248, 248, 255, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (220, 220, 220, 255).</summary>
        public static ColorByte Gainsboro => new ColorByte(220, 220, 220, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 0, 255, 255).</summary>
        public static ColorByte Fuchsia => new ColorByte(255, 0, 255, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (34, 139, 34, 255).</summary>
        public static ColorByte ForestGreen => new ColorByte(34, 139, 34, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 250, 240, 255).</summary>
        public static ColorByte FloralWhite => new ColorByte(255, 250, 240, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (240, 255, 240, 255).</summary>
        public static ColorByte Honeydew => new ColorByte(240, 255, 240, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (30, 144, 255, 255).</summary>
        public static ColorByte DodgerBlue => new ColorByte(30, 144, 255, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (178, 34, 34, 255).</summary>
        public static ColorByte Firebrick => new ColorByte(178, 34, 34, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 140, 0, 255).</summary>
        public static ColorByte DarkOrange => new ColorByte(255, 140, 0, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (153, 50, 204, 255).</summary>
        public static ColorByte DarkOrchid => new ColorByte(153, 50, 204, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (139, 0, 0, 255).</summary>
        public static ColorByte DarkRed => new ColorByte(139, 0, 0, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (233, 150, 122, 255).</summary>
        public static ColorByte DarkSalmon => new ColorByte(233, 150, 122, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (143, 188, 139, 255).</summary>
        public static ColorByte DarkSeaGreen => new ColorByte(143, 188, 139, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (154, 205, 50, 255).</summary>
        public static ColorByte YellowGreen => new ColorByte(154, 205, 50, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (47, 79, 79, 255).</summary>
        public static ColorByte DarkSlateGray => new ColorByte(47, 79, 79, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 206, 209, 255).</summary>
        public static ColorByte DarkTurquoise => new ColorByte(0, 206, 209, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (148, 0, 211, 255).</summary>
        public static ColorByte DarkViolet => new ColorByte(148, 0, 211, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 20, 147, 255).</summary>
        public static ColorByte DeepPink => new ColorByte(255, 20, 147, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 191, 255, 255).</summary>
        public static ColorByte DeepSkyBlue => new ColorByte(0, 191, 255, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (105, 105, 105, 255).</summary>
        public static ColorByte DimGray => new ColorByte(105, 105, 105, 255);
        /// <summary>Gets the system color with (R, G, B, A) = (72, 61, 139, 255).</summary>
        public static ColorByte DarkSlateBlue => new ColorByte(72, 61, 139, 255);
    }
}
