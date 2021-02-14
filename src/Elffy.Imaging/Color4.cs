#nullable enable
using Elffy.Effective.Unsafes;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NVec4 = System.Numerics.Vector4;

namespace Elffy
{
    [DebuggerDisplay("{DebugView}")]
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

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebugView => $"(R, G, B, A) = ({ToByte(R)}, {ToByte(G)}, {ToByte(B)}, {ToByte(A)}) = ({R}, {G}, {B}, {A})";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color4(float r, float g, float b) => (R, G, B, A) = (r, g, b, 1f);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color4(float value) => (R, G, B, A) = (value, value, value, 1f);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color4(float r, float g, float b, float a) => (R, G, B, A) = (r, g, b, a);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color4(in Color3 color) => (R, G, B, A) = (color.R, color.G, color.B, 1f);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Color4(in Color3 color, float a) => (R, G, B, A) = (color.R, color.G, color.B, a);

        [EditorBrowsable(EditorBrowsableState.Never)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Deconstruct(out float r, out float g, out float b, out float a) => (r, g, b, a) = (R, G, B, A);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Color4 PreMultiplied()
        {
            return UnsafeEx.As<NVec4, Color4>(
                UnsafeEx.As<Color4, NVec4>(in this) * new NVec4(A, A, A, 1f)
                );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PreMultiply()
        {
            this = UnsafeEx.As<NVec4, Color4>(
                UnsafeEx.As<Color4, NVec4>(in this) * new NVec4(A, A, A, 1f)
                );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ColorByte ToColorByte()
        {
            return new ColorByte((byte)(R * byte.MaxValue), (byte)(G * byte.MaxValue), (byte)(B * byte.MaxValue), (byte)(A * byte.MaxValue));
        }

        private static byte ToByte(float value)
        {
            var tmp = value * byte.MaxValue;
            return (tmp < 0f) ? (byte)0 :
                   (tmp > (float)byte.MaxValue) ? byte.MaxValue : (byte)tmp;
        }

        public readonly override bool Equals(object? obj) => obj is Color4 color && Equals(color);

        public readonly bool Equals(Color4 other) => R == other.R && G == other.G && B == other.B && A == other.A;

        public readonly override int GetHashCode() => HashCode.Combine(R, G, B, A);
        public readonly override string ToString() => DebugView;

        public static bool operator ==(in Color4 left, in Color4 right) => left.Equals(right);

        public static bool operator !=(in Color4 left, in Color4 right) => !(left == right);

        public static explicit operator Color4(in ColorByte color) => color.ToColor4();
        public static explicit operator ColorByte(in Color4 color) => color.ToColorByte();


        /// <summary>Gets the system color with (R, G, B, A) = (102, 205, 170, 255).</summary>
        public static Color4 MediumAquamarine => new Color4(0.4f, 0.8039216f, 0.6666667f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 0, 205, 255).</summary>
        public static Color4 MediumBlue => new Color4(0f, 0f, 0.8039216f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (186, 85, 211, 255).</summary>
        public static Color4 MediumOrchid => new Color4(0.7294118f, 0.33333334f, 0.827451f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (147, 112, 219, 255).</summary>
        public static Color4 MediumPurple => new Color4(0.5764706f, 0.4392157f, 0.85882354f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (60, 179, 113, 255).</summary>
        public static Color4 MediumSeaGreen => new Color4(0.23529412f, 0.7019608f, 0.44313726f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (123, 104, 238, 255).</summary>
        public static Color4 MediumSlateBlue => new Color4(0.48235294f, 0.40784314f, 0.93333334f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 250, 154, 255).</summary>
        public static Color4 MediumSpringGreen => new Color4(0f, 0.98039216f, 0.6039216f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (72, 209, 204, 255).</summary>
        public static Color4 MediumTurquoise => new Color4(0.28235295f, 0.81960785f, 0.8f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (245, 255, 250, 255).</summary>
        public static Color4 MintCream => new Color4(0.9607843f, 1f, 0.98039216f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (25, 25, 112, 255).</summary>
        public static Color4 MidnightBlue => new Color4(0.09803922f, 0.09803922f, 0.4392157f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (128, 0, 0, 255).</summary>
        public static Color4 Maroon => new Color4(0.5019608f, 0f, 0f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 228, 225, 255).</summary>
        public static Color4 MistyRose => new Color4(1f, 0.89411765f, 0.88235295f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 228, 181, 255).</summary>
        public static Color4 Moccasin => new Color4(1f, 0.89411765f, 0.70980394f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 222, 173, 255).</summary>
        public static Color4 NavajoWhite => new Color4(1f, 0.87058824f, 0.6784314f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 0, 128, 255).</summary>
        public static Color4 Navy => new Color4(0f, 0f, 0.5019608f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (253, 245, 230, 255).</summary>
        public static Color4 OldLace => new Color4(0.99215686f, 0.9607843f, 0.9019608f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (199, 21, 133, 255).</summary>
        public static Color4 MediumVioletRed => new Color4(0.78039217f, 0.08235294f, 0.52156866f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 0, 255, 255).</summary>
        public static Color4 Magenta => new Color4(1f, 0f, 1f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 255, 0, 255).</summary>
        public static Color4 Lime => new Color4(0f, 1f, 0f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (50, 205, 50, 255).</summary>
        public static Color4 LimeGreen => new Color4(0.19607843f, 0.8039216f, 0.19607843f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 240, 245, 255).</summary>
        public static Color4 LavenderBlush => new Color4(1f, 0.9411765f, 0.9607843f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (124, 252, 0, 255).</summary>
        public static Color4 LawnGreen => new Color4(0.4862745f, 0.9882353f, 0f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 250, 205, 255).</summary>
        public static Color4 LemonChiffon => new Color4(1f, 0.98039216f, 0.8039216f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (173, 216, 230, 255).</summary>
        public static Color4 LightBlue => new Color4(0.6784314f, 0.84705883f, 0.9019608f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (240, 128, 128, 255).</summary>
        public static Color4 LightCoral => new Color4(0.9411765f, 0.5019608f, 0.5019608f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (224, 255, 255, 255).</summary>
        public static Color4 LightCyan => new Color4(0.8784314f, 1f, 1f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (250, 250, 210, 255).</summary>
        public static Color4 LightGoldenrodYellow => new Color4(0.98039216f, 0.98039216f, 0.8235294f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (144, 238, 144, 255).</summary>
        public static Color4 LightGreen => new Color4(0.5647059f, 0.93333334f, 0.5647059f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (211, 211, 211, 255).</summary>
        public static Color4 LightGray => new Color4(0.827451f, 0.827451f, 0.827451f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 182, 193, 255).</summary>
        public static Color4 LightPink => new Color4(1f, 0.7137255f, 0.75686276f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 160, 122, 255).</summary>
        public static Color4 LightSalmon => new Color4(1f, 0.627451f, 0.47843137f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (32, 178, 170, 255).</summary>
        public static Color4 LightSeaGreen => new Color4(0.1254902f, 0.69803923f, 0.6666667f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (135, 206, 250, 255).</summary>
        public static Color4 LightSkyBlue => new Color4(0.5294118f, 0.80784315f, 0.98039216f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (119, 136, 153, 255).</summary>
        public static Color4 LightSlateGray => new Color4(0.46666667f, 0.53333336f, 0.6f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (176, 196, 222, 255).</summary>
        public static Color4 LightSteelBlue => new Color4(0.6901961f, 0.76862746f, 0.87058824f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 255, 224, 255).</summary>
        public static Color4 LightYellow => new Color4(1f, 1f, 0.8784314f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (128, 128, 0, 255).</summary>
        public static Color4 Olive => new Color4(0.5019608f, 0.5019608f, 0f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (250, 240, 230, 255).</summary>
        public static Color4 Linen => new Color4(0.98039216f, 0.9411765f, 0.9019608f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (107, 142, 35, 255).</summary>
        public static Color4 OliveDrab => new Color4(0.41960785f, 0.5568628f, 0.13725491f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (218, 112, 214, 255).</summary>
        public static Color4 Orchid => new Color4(0.85490197f, 0.4392157f, 0.8392157f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 69, 0, 255).</summary>
        public static Color4 OrangeRed => new Color4(1f, 0.27058825f, 0f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (192, 192, 192, 255).</summary>
        public static Color4 Silver => new Color4(0.7529412f, 0.7529412f, 0.7529412f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (135, 206, 235, 255).</summary>
        public static Color4 SkyBlue => new Color4(0.5294118f, 0.80784315f, 0.92156863f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (106, 90, 205, 255).</summary>
        public static Color4 SlateBlue => new Color4(0.41568628f, 0.3529412f, 0.8039216f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (112, 128, 144, 255).</summary>
        public static Color4 SlateGray => new Color4(0.4392157f, 0.5019608f, 0.5647059f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 250, 250, 255).</summary>
        public static Color4 Snow => new Color4(1f, 0.98039216f, 0.98039216f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 255, 127, 255).</summary>
        public static Color4 SpringGreen => new Color4(0f, 1f, 0.49803922f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (70, 130, 180, 255).</summary>
        public static Color4 SteelBlue => new Color4(0.27450982f, 0.50980395f, 0.7058824f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (160, 82, 45, 255).</summary>
        public static Color4 Sienna => new Color4(0.627451f, 0.32156864f, 0.1764706f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (210, 180, 140, 255).</summary>
        public static Color4 Tan => new Color4(0.8235294f, 0.7058824f, 0.54901963f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (216, 191, 216, 255).</summary>
        public static Color4 Thistle => new Color4(0.84705883f, 0.7490196f, 0.84705883f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 99, 71, 255).</summary>
        public static Color4 Tomato => new Color4(1f, 0.3882353f, 0.2784314f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (64, 224, 208, 255).</summary>
        public static Color4 Turquoise => new Color4(0.2509804f, 0.8784314f, 0.8156863f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (238, 130, 238, 255).</summary>
        public static Color4 Violet => new Color4(0.93333334f, 0.50980395f, 0.93333334f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (245, 222, 179, 255).</summary>
        public static Color4 Wheat => new Color4(0.9607843f, 0.87058824f, 0.7019608f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 255, 255, 255).</summary>
        public static Color4 White => new Color4(1f, 1f, 1f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (245, 245, 245, 255).</summary>
        public static Color4 WhiteSmoke => new Color4(0.9607843f, 0.9607843f, 0.9607843f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 128, 128, 255).</summary>
        public static Color4 Teal => new Color4(0f, 0.5019608f, 0.5019608f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 245, 238, 255).</summary>
        public static Color4 SeaShell => new Color4(1f, 0.9607843f, 0.93333334f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (46, 139, 87, 255).</summary>
        public static Color4 SeaGreen => new Color4(0.18039216f, 0.54509807f, 0.34117648f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (244, 164, 96, 255).</summary>
        public static Color4 SandyBrown => new Color4(0.95686275f, 0.6431373f, 0.3764706f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (230, 230, 250, 255).</summary>
        public static Color4 Lavender => new Color4(0.9019608f, 0.9019608f, 0.98039216f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (238, 232, 170, 255).</summary>
        public static Color4 PaleGoldenrod => new Color4(0.93333334f, 0.9098039f, 0.6666667f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (152, 251, 152, 255).</summary>
        public static Color4 PaleGreen => new Color4(0.59607846f, 0.9843137f, 0.59607846f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (175, 238, 238, 255).</summary>
        public static Color4 PaleTurquoise => new Color4(0.6862745f, 0.93333334f, 0.93333334f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (219, 112, 147, 255).</summary>
        public static Color4 PaleVioletRed => new Color4(0.85882354f, 0.4392157f, 0.5764706f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 239, 213, 255).</summary>
        public static Color4 PapayaWhip => new Color4(1f, 0.9372549f, 0.8352941f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 218, 185, 255).</summary>
        public static Color4 PeachPuff => new Color4(1f, 0.85490197f, 0.7254902f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (205, 133, 63, 255).</summary>
        public static Color4 Peru => new Color4(0.8039216f, 0.52156866f, 0.24705882f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 192, 203, 255).</summary>
        public static Color4 Pink => new Color4(1f, 0.7529412f, 0.79607844f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (221, 160, 221, 255).</summary>
        public static Color4 Plum => new Color4(0.8666667f, 0.627451f, 0.8666667f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (176, 224, 230, 255).</summary>
        public static Color4 PowderBlue => new Color4(0.6901961f, 0.8784314f, 0.9019608f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (128, 0, 128, 255).</summary>
        public static Color4 Purple => new Color4(0.5019608f, 0f, 0.5019608f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 0, 0, 255).</summary>
        public static Color4 Red => new Color4(1f, 0f, 0f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (188, 143, 143, 255).</summary>
        public static Color4 RosyBrown => new Color4(0.7372549f, 0.56078434f, 0.56078434f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (65, 105, 225, 255).</summary>
        public static Color4 RoyalBlue => new Color4(0.25490198f, 0.4117647f, 0.88235295f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (139, 69, 19, 255).</summary>
        public static Color4 SaddleBrown => new Color4(0.54509807f, 0.27058825f, 0.07450981f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (250, 128, 114, 255).</summary>
        public static Color4 Salmon => new Color4(0.98039216f, 0.5019608f, 0.44705883f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 165, 0, 255).</summary>
        public static Color4 Orange => new Color4(1f, 0.64705884f, 0f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (240, 230, 140, 255).</summary>
        public static Color4 Khaki => new Color4(0.9411765f, 0.9019608f, 0.54901963f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (205, 92, 92, 255).</summary>
        public static Color4 IndianRed => new Color4(0.8039216f, 0.36078432f, 0.36078432f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (75, 0, 130, 255).</summary>
        public static Color4 Indigo => new Color4(0.29411766f, 0f, 0.50980395f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (169, 169, 169, 255).</summary>
        public static Color4 DarkGray => new Color4(0.6627451f, 0.6627451f, 0.6627451f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (184, 134, 11, 255).</summary>
        public static Color4 DarkGoldenrod => new Color4(0.72156864f, 0.5254902f, 0.043137256f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 139, 139, 255).</summary>
        public static Color4 DarkCyan => new Color4(0f, 0.54509807f, 0.54509807f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 0, 139, 255).</summary>
        public static Color4 DarkBlue => new Color4(0f, 0f, 0.54509807f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 255, 255, 255).</summary>
        public static Color4 Cyan => new Color4(0f, 1f, 1f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (220, 20, 60, 255).</summary>
        public static Color4 Crimson => new Color4(0.8627451f, 0.078431375f, 0.23529412f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 248, 220, 255).</summary>
        public static Color4 Cornsilk => new Color4(1f, 0.972549f, 0.8627451f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (100, 149, 237, 255).</summary>
        public static Color4 CornflowerBlue => new Color4(0.39215687f, 0.58431375f, 0.92941177f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 255, 240, 255).</summary>
        public static Color4 Ivory => new Color4(1f, 1f, 0.9411765f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (210, 105, 30, 255).</summary>
        public static Color4 Chocolate => new Color4(0.8235294f, 0.4117647f, 0.11764706f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (127, 255, 0, 255).</summary>
        public static Color4 Chartreuse => new Color4(0.49803922f, 1f, 0f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (95, 158, 160, 255).</summary>
        public static Color4 CadetBlue => new Color4(0.37254903f, 0.61960787f, 0.627451f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 100, 0, 255).</summary>
        public static Color4 DarkGreen => new Color4(0f, 0.39215687f, 0f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (222, 184, 135, 255).</summary>
        public static Color4 BurlyWood => new Color4(0.87058824f, 0.72156864f, 0.5294118f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (138, 43, 226, 255).</summary>
        public static Color4 BlueViolet => new Color4(0.5411765f, 0.16862746f, 0.8862745f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 0, 255, 255).</summary>
        public static Color4 Blue => new Color4(0f, 0f, 1f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 235, 205, 255).</summary>
        public static Color4 BlanchedAlmond => new Color4(1f, 0.92156863f, 0.8039216f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 0, 0, 255).</summary>
        public static Color4 Black => new Color4(0f, 0f, 0f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 228, 196, 255).</summary>
        public static Color4 Bisque => new Color4(1f, 0.89411765f, 0.76862746f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (245, 245, 220, 255).</summary>
        public static Color4 Beige => new Color4(0.9607843f, 0.9607843f, 0.8627451f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (240, 255, 255, 255).</summary>
        public static Color4 Azure => new Color4(0.9411765f, 1f, 1f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (127, 255, 212, 255).</summary>
        public static Color4 Aquamarine => new Color4(0.49803922f, 1f, 0.83137256f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 255, 255, 255).</summary>
        public static Color4 Aqua => new Color4(0f, 1f, 1f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (250, 235, 215, 255).</summary>
        public static Color4 AntiqueWhite => new Color4(0.98039216f, 0.92156863f, 0.84313726f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (240, 248, 255, 255).</summary>
        public static Color4 AliceBlue => new Color4(0.9411765f, 0.972549f, 1f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 255, 255, 0).</summary>
        public static Color4 Transparent => new Color4(1f, 1f, 1f, 0f);
        /// <summary>Gets the system color with (R, G, B, A) = (165, 42, 42, 255).</summary>
        public static Color4 Brown => new Color4(0.64705884f, 0.16470589f, 0.16470589f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (189, 183, 107, 255).</summary>
        public static Color4 DarkKhaki => new Color4(0.7411765f, 0.7176471f, 0.41960785f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 127, 80, 255).</summary>
        public static Color4 Coral => new Color4(1f, 0.49803922f, 0.3137255f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (85, 107, 47, 255).</summary>
        public static Color4 DarkOliveGreen => new Color4(0.33333334f, 0.41960785f, 0.18431373f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 255, 0, 255).</summary>
        public static Color4 Yellow => new Color4(1f, 1f, 0f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 105, 180, 255).</summary>
        public static Color4 HotPink => new Color4(1f, 0.4117647f, 0.7058824f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (139, 0, 139, 255).</summary>
        public static Color4 DarkMagenta => new Color4(0.54509807f, 0f, 0.54509807f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (173, 255, 47, 255).</summary>
        public static Color4 GreenYellow => new Color4(0.6784314f, 1f, 0.18431373f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 128, 0, 255).</summary>
        public static Color4 Green => new Color4(0f, 0.5019608f, 0f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (128, 128, 128, 255).</summary>
        public static Color4 Gray => new Color4(0.5019608f, 0.5019608f, 0.5019608f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (218, 165, 32, 255).</summary>
        public static Color4 Goldenrod => new Color4(0.85490197f, 0.64705884f, 0.1254902f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 215, 0, 255).</summary>
        public static Color4 Gold => new Color4(1f, 0.84313726f, 0f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (248, 248, 255, 255).</summary>
        public static Color4 GhostWhite => new Color4(0.972549f, 0.972549f, 1f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (220, 220, 220, 255).</summary>
        public static Color4 Gainsboro => new Color4(0.8627451f, 0.8627451f, 0.8627451f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 0, 255, 255).</summary>
        public static Color4 Fuchsia => new Color4(1f, 0f, 1f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (34, 139, 34, 255).</summary>
        public static Color4 ForestGreen => new Color4(0.13333334f, 0.54509807f, 0.13333334f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 250, 240, 255).</summary>
        public static Color4 FloralWhite => new Color4(1f, 0.98039216f, 0.9411765f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (240, 255, 240, 255).</summary>
        public static Color4 Honeydew => new Color4(0.9411765f, 1f, 0.9411765f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (30, 144, 255, 255).</summary>
        public static Color4 DodgerBlue => new Color4(0.11764706f, 0.5647059f, 1f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (178, 34, 34, 255).</summary>
        public static Color4 Firebrick => new Color4(0.69803923f, 0.13333334f, 0.13333334f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 140, 0, 255).</summary>
        public static Color4 DarkOrange => new Color4(1f, 0.54901963f, 0f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (153, 50, 204, 255).</summary>
        public static Color4 DarkOrchid => new Color4(0.6f, 0.19607843f, 0.8f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (139, 0, 0, 255).</summary>
        public static Color4 DarkRed => new Color4(0.54509807f, 0f, 0f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (233, 150, 122, 255).</summary>
        public static Color4 DarkSalmon => new Color4(0.9137255f, 0.5882353f, 0.47843137f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (143, 188, 139, 255).</summary>
        public static Color4 DarkSeaGreen => new Color4(0.56078434f, 0.7372549f, 0.54509807f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (154, 205, 50, 255).</summary>
        public static Color4 YellowGreen => new Color4(0.6039216f, 0.8039216f, 0.19607843f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (47, 79, 79, 255).</summary>
        public static Color4 DarkSlateGray => new Color4(0.18431373f, 0.30980393f, 0.30980393f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 206, 209, 255).</summary>
        public static Color4 DarkTurquoise => new Color4(0f, 0.80784315f, 0.81960785f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (148, 0, 211, 255).</summary>
        public static Color4 DarkViolet => new Color4(0.5803922f, 0f, 0.827451f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (255, 20, 147, 255).</summary>
        public static Color4 DeepPink => new Color4(1f, 0.078431375f, 0.5764706f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (0, 191, 255, 255).</summary>
        public static Color4 DeepSkyBlue => new Color4(0f, 0.7490196f, 1f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (105, 105, 105, 255).</summary>
        public static Color4 DimGray => new Color4(0.4117647f, 0.4117647f, 0.4117647f, 1f);
        /// <summary>Gets the system color with (R, G, B, A) = (72, 61, 139, 255).</summary>
        public static Color4 DarkSlateBlue => new Color4(0.28235295f, 0.23921569f, 0.54509807f, 1f);
    }
}
