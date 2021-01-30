#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Imaging
{
    [StructLayout(LayoutKind.Explicit)]
    public unsafe readonly struct ReadOnlyRawImage : IEquatable<ReadOnlyRawImage>
    {
        // Fields layout must be same as RawImage.

        /// <summary>Get width of image</summary>
        [FieldOffset(0)]
        public readonly int Width;
        /// <summary>Get height of image</summary>
        [FieldOffset(4)]
        public readonly int Height;
        [FieldOffset(8)]
        private readonly ColorByte* _pixels;

        /// <summary>Get pixels raw data, which is layouted as (R, G, B, A), each pixel is <see cref="ColorByte"/>.</summary>
        public ref readonly ColorByte Pixels => ref Unsafe.AsRef<ColorByte>(_pixels);

        /// <summary>Get pixel of specified (x, y)</summary>
        /// <param name="x">x index (column line)</param>
        /// <param name="y">y index (row line)</param>
        /// <returns>pixel</returns>
        public ref readonly ColorByte this[int x, int y]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if((uint)x >= (uint)Width) {
                    ThrowOutOfRange(nameof(x));
                }
                if((uint)y >= (uint)Height) {
                    ThrowOutOfRange(nameof(y));
                }
                return ref _pixels[y * Width + x];
            }
        }

        /// <summary>Get empty <see cref="ReadOnlyRawImage"/></summary>
        public static ReadOnlyRawImage Empty => default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyRawImage(int width, int height, IntPtr pixels) : this(width, height, (ColorByte*)pixels)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyRawImage(int width, int height, ColorByte* pixels)
        {
            Width = width;
            Height = height;
            _pixels = pixels;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<ColorByte> AsSpan()
        {
            return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef<ColorByte>(_pixels), Width * Height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<ColorByte> GetRowLine(int row)
        {
            if((uint)row >= (uint)Height) {
                ThrowOutOfRange(nameof(row));
            }
            return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef<ColorByte>(_pixels + row * Width), Width);
        }

        public override bool Equals(object? obj) => obj is ReadOnlyRawImage image && Equals(image);

        public bool Equals(ReadOnlyRawImage other) => Width == other.Width && Height == other.Height && _pixels == other._pixels;

        public override int GetHashCode() => HashCode.Combine(Width, Height, (IntPtr)_pixels);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlyRawImage(RawImage image) => Unsafe.As<RawImage, ReadOnlyRawImage>(ref image);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in ReadOnlyRawImage left, in ReadOnlyRawImage right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in ReadOnlyRawImage left, in ReadOnlyRawImage right) => !(left == right);

        [DoesNotReturn]
        private static void ThrowOutOfRange(string message) => throw new ArgumentOutOfRangeException(message);
    }
}
