#nullable enable
using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Elffy.Markup;

namespace Elffy
{
    /// <summary>Color structure with RGBA bytes format</summary>
    [DebuggerDisplay("{DebugView,nq}")]
    [StructLayout(LayoutKind.Explicit)]
    [UseLiteralMarkup]
    [LiteralMarkupPattern(HexCodePattern, HexCodeEmit)]
    public partial struct ColorByte : IEquatable<ColorByte>
    {
        // lang=regex
        private const string HexCodePattern = @"^#[0-9a-fA-F]{6}([0-9a-fA-F]{2})?$";
        private const string HexCodeEmit = @"global::Elffy.ColorByte.FromHexCode(""$0"")";

        [FieldOffset(0)]
        public byte R;
        [FieldOffset(1)]
        public byte G;
        [FieldOffset(2)]
        public byte B;
        [FieldOffset(3)]
        public byte A;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebugView => $"(R, G, B, A) = ({R}, {G}, {B}, {A})";

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
        public readonly Color3 ToColor3() => new((float)R / byte.MaxValue, (float)G / byte.MaxValue, (float)B / byte.MaxValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Color4 ToColor4() => new((float)R / byte.MaxValue, (float)G / byte.MaxValue, (float)B / byte.MaxValue, (float)A / byte.MaxValue);

        public readonly override bool Equals(object? obj) => obj is ColorByte color && Equals(color);

        public readonly bool Equals(ColorByte other) => R == other.R && G == other.G && B == other.B && A == other.A;

        public readonly override int GetHashCode() => HashCode.Combine(R, G, B, A);

        public static bool operator ==(in ColorByte left, in ColorByte right) => left.Equals(right);

        public static bool operator !=(in ColorByte left, in ColorByte right) => !(left == right);

        public readonly override string ToString() => DebugView;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ColorByte FromHexCode(string hexCode) => FromHexCode(hexCode.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ColorByte FromHexCode(ReadOnlySpan<char> hexCode) =>
            TryFromHexCode(hexCode, out var color) ? color : throw new FormatException($"Invalid Format: '{hexCode.ToString()}'");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryFromHexCode(string hex, out ColorByte color) => TryFromHexCode(hex.AsSpan(), out color);

        public static bool TryFromHexCode(ReadOnlySpan<char> hexCode, out ColorByte color)
        {
            if(hexCode.Length < 7) { goto FAILURE; }
            if(hexCode[0] != '#') { goto FAILURE; }
            if(HexToByte(hexCode[1], hexCode[2], out color.R) == false) { goto FAILURE; }
            if(HexToByte(hexCode[3], hexCode[4], out color.G) == false) { goto FAILURE; }
            if(HexToByte(hexCode[5], hexCode[6], out color.B) == false) { goto FAILURE; }

            if(hexCode.Length == 9) {
                if(HexToByte(hexCode[7], hexCode[8], out color.A) == false) { goto FAILURE; }
            }
            else {
                color.A = byte.MaxValue;
            }
            return true;

        FAILURE:
            color = default;
            return false;

            static bool HexToByte(char upper, char lower, out byte value)
            {
                if(AtoiHex(upper, out var a) == false) {
                    value = default;
                    return false;
                }
                value = (byte)(a << 4);
                if(AtoiHex(lower, out var b) == false) {
                    value = default;
                    return false;
                }
                value += b;
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool AtoiHex(char c, out byte value)
            {
                var n = c - '0';
                if((uint)n <= 9) {
                    value = (byte)n;
                    return true;
                }
                var l = c - 'A';
                if((uint)l <= 5) {
                    value = (byte)(l + 10);
                    return true;
                }
                var s = c - 'a';
                if((uint)s <= 5) {
                    value = (byte)(s + 10);
                    return true;
                }
                value = default;
                return false;
            }

        }

        /// <summary>Try to get color from web color name, which must be small letter.</summary>
        /// <param name="name">web color name</param>
        /// <param name="color">color</param>
        /// <returns>success or not</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryFromWebColorName(string name, out ColorByte color) => WebColors.TryGetColorByte(name, out color);

        /// <summary>Get color from web color name, which must be small letter.</summary>
        /// <param name="name">web color name</param>
        /// <returns>color</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ColorByte FromWebColorName(string name) =>
            WebColors.TryGetColorByte(name, out var color) ? color :
            throw new ArgumentException($"Web color name is not defined. The name must be small letter. (name='{name}')");

        public static bool IsWebColorDefined(string name) => WebColors.IsDefined(name);
    }
}
