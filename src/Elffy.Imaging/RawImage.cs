#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Imaging
{
    [StructLayout(LayoutKind.Explicit)]
    public unsafe readonly struct RawImage : IEquatable<RawImage>
    {
        // Fields layout must be same as ReadOnlyRawImage.

        /// <summary>Get width of image</summary>
        [FieldOffset(0)]
        public readonly int Width;
        /// <summary>Get height of image</summary>
        [FieldOffset(4)]
        public readonly int Height;
        /// <summary>Get pixels raw data, which is layouted as (R, G, B, A), each pixel is 4 bytes, each channel is 1 byte.</summary>
        [FieldOffset(8)]
        public readonly ColorByte* Pixels;

        /// <summary>Get or set pixel of specified (x, y)</summary>
        /// <param name="x">x index (column line)</param>
        /// <param name="y">y index (row line)</param>
        /// <returns>pixel</returns>
        public ref ColorByte this[int x, int y]
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
                return ref Pixels[y * Width + x];
            }
        }

        /// <summary>Get empty <see cref="RawImage"/></summary>
        public static RawImage Empty => default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RawImage(int width, int height, IntPtr pixels) : this(width, height, (ColorByte*)pixels)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RawImage(int width, int height, ColorByte* pixels)
        {
            if(width < 0) {
                ThrowOutOfRange(nameof(width));
            }
            if(height < 0) {
                ThrowOutOfRange(nameof(height));
            }
            Width = width;
            Height = height;
            Pixels = pixels;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<ColorByte> AsSpan()
        {
            return MemoryMarshal.CreateSpan(ref Unsafe.AsRef<ColorByte>(Pixels), Width * Height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Span<ColorByte> GetRowLine(int row)
        {
            if((uint)row >= (uint)Height) {
                ThrowOutOfRange(nameof(row));
            }
            ref var head = ref Unsafe.AsRef<ColorByte>(Pixels + row * Width);
            return MemoryMarshal.CreateSpan(ref head, Width);
        }

        public override bool Equals(object? obj) => obj is RawImage image && Equals(image);

        public bool Equals(RawImage other) => Width == other.Width && Height == other.Height && Pixels == other.Pixels;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in RawImage left, in RawImage right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in RawImage left, in RawImage right) => !(left == right);

        public override int GetHashCode() => HashCode.Combine(Width, Height, (IntPtr)Pixels);

        [DoesNotReturn]
        private static void ThrowOutOfRange(string message) => throw new ArgumentOutOfRangeException(message);
    }
}
