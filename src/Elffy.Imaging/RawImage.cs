#nullable enable
using System;

namespace Elffy.Imaging
{
    public unsafe readonly struct RawImage : IEquatable<RawImage>
    {
        /// <summary>Get width of image</summary>
        public readonly int Width;
        /// <summary>Get height of image</summary>
        public readonly int Height;
        /// <summary>Get pixels raw data, which is layouted as (R, G, B, A), each pixel is 4 bytes, each channel is 1 byte.</summary>
        public readonly byte* Pixels;

        public RawImage(int width, int height, byte* pixels)
        {
            Width = width;
            Height = height;
            Pixels = pixels;
        }

        public override bool Equals(object? obj)
        {
            return obj is RawImage image && Equals(image);
        }

        public bool Equals(RawImage other)
        {
            return Width == other.Width &&
                   Height == other.Height &&
                   Pixels == other.Pixels;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Width, Height, (IntPtr)Pixels);
        }
    }
}
