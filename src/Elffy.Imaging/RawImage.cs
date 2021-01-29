#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Imaging
{
    public unsafe readonly struct RawImage : IEquatable<RawImage>
    {
        /// <summary>Get width of image</summary>
        public readonly int Width;
        /// <summary>Get height of image</summary>
        public readonly int Height;
        /// <summary>Get pixels raw data, which is layouted as (R, G, B, A), each pixel is 4 bytes, each channel is 1 byte.</summary>
        public readonly ColorByte* Pixels;

        public static RawImage Empty => default;

        public ref ColorByte this[int x, int y]
        {
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

        public RawImage(int width, int height, IntPtr pixels) : this(width, height, (ColorByte*)pixels)
        {

        }

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

        public Span<ColorByte> AsSpan()
        {
            return MemoryMarshal.CreateSpan(ref Unsafe.AsRef<ColorByte>(Pixels), Width * Height);
        }

        public override bool Equals(object? obj) => obj is RawImage image && Equals(image);

        public bool Equals(RawImage other) => Width == other.Width && Height == other.Height && Pixels == other.Pixels;

        public override int GetHashCode() => HashCode.Combine(Width, Height, (IntPtr)Pixels);

        [DoesNotReturn]
        private static void ThrowOutOfRange(string message) => throw new ArgumentOutOfRangeException(message);
    }
}
