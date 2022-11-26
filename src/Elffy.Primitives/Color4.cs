#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Elffy.Markup;
using NVec4 = System.Numerics.Vector4;

namespace Elffy
{
    [DebuggerDisplay("{DebugView}")]
    [StructLayout(LayoutKind.Explicit)]
    [UseLiteralMarkup]
    [LiteralMarkupPattern(HexCodePattern, HexCodeEmit)]
    public partial struct Color4 : IEquatable<Color4>, IStringConvertible<Color4>
    {
        // lang=regex
        private const string HexCodePattern = @"^#[0-9a-fA-F]{6}([0-9a-fA-F]{2})?$";
        private const string HexCodeEmit = @"global::Elffy.Color4.FromHexCode(""$0"")";

        [FieldOffset(0)]
        public float R;
        [FieldOffset(4)]
        public float G;
        [FieldOffset(8)]
        public float B;
        [FieldOffset(12)]
        public float A;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebugView
            => $"(R, G, B, A) = ({R}, {G}, {B}, {A}) = ({(byte)(R * byte.MaxValue)}, {(byte)(G * byte.MaxValue)}, {(byte)(B * byte.MaxValue)}, {(byte)(A * byte.MaxValue)})";

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
        public readonly Color3 ToColor3() => new(R, G, B);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ColorByte ToColorByte() => new()
        {
            R = (byte)MathF.Max(0, MathF.Min(byte.MaxValue, R * byte.MaxValue)),
            G = (byte)MathF.Max(0, MathF.Min(byte.MaxValue, G * byte.MaxValue)),
            B = (byte)MathF.Max(0, MathF.Min(byte.MaxValue, B * byte.MaxValue)),
            A = (byte)MathF.Max(0, MathF.Min(byte.MaxValue, A * byte.MaxValue)),
        };

        public readonly override bool Equals(object? obj) => obj is Color4 color && Equals(color);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Color4 other) => AsNVec4(this) == AsNVec4(other);

        public readonly override int GetHashCode() => HashCode.Combine(R, G, B, A);
        public readonly override string ToString() => DebugView;

        public static bool operator ==(in Color4 left, in Color4 right) => left.Equals(right);

        public static bool operator !=(in Color4 left, in Color4 right) => !(left == right);

        public static explicit operator Color4(in ColorByte color) => color.ToColor4();
        public static explicit operator ColorByte(in Color4 color) => color.ToColorByte();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color4 FromHexCode(string hexCode) => FromHexCode(hexCode.AsSpan());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color4 FromHexCode(ReadOnlySpan<char> hexCode) =>
            TryFromHexCode(hexCode, out var color) ? color : throw new FormatException($"Invalid Format: '{hexCode.ToString()}'");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryFromHexCode(string hex, out Color4 color) => TryFromHexCode(hex.AsSpan(), out color);

        public static bool TryFromHexCode(ReadOnlySpan<char> hexCode, out Color4 color)
        {
            if(ColorByte.TryFromHexCode(hexCode, out var colorByte)) {
                color = colorByte.ToColor4();
                return true;
            }
            color = default;
            return false;
        }

        /// <summary>Try to get color from web color name, which must be small letter.</summary>
        /// <param name="name">web color name</param>
        /// <param name="color">color</param>
        /// <returns>success or not</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryFromWebColorName(string name, out Color4 color) => WebColors.TryGetColor4(name, out color);

        /// <summary>Get color from web color name, which must be small letter.</summary>
        /// <param name="name">web color name</param>
        /// <returns>color</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color4 FromWebColorName(string name) =>
            WebColors.TryGetColor4(name, out var color) ? color :
            throw new ArgumentException($"Web color name is not defined. The name must be small letter. (name='{name}')");

        public static bool IsWebColorDefined(string name) => WebColors.IsDefined(name);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref readonly NVec4 AsNVec4(in Color4 vec) => ref Unsafe.As<Color4, NVec4>(ref Unsafe.AsRef(vec));

        public static Color4 Convert(string value)
        {
            ArgumentException.ThrowIfNullOrEmpty(value);
            if(TryFromHexCode(value, out var hexColor)) {
                return hexColor;
            }
            if(TryFromWebColorName(value, out var webColor)) {
                return webColor;
            }
            throw new FormatException(value);
        }
    }
}
