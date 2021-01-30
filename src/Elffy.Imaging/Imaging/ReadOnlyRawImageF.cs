#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace Elffy.Imaging
{
    [StructLayout(LayoutKind.Explicit)]
    public unsafe readonly struct ReadOnlyRawImageF : IEquatable<ReadOnlyRawImageF>
    {
        // Fields layout must be same as RawImageF.

        /// <summary>Get width of image</summary>
        [FieldOffset(0)]
        public readonly int Width;
        /// <summary>Get height of image</summary>
        [FieldOffset(4)]
        public readonly int Height;
        [FieldOffset(8)]
        public readonly Color4* _pixels;

        /// <summary>Get pixels raw data, which is layouted as (R, G, B, A), each pixel is <see cref="Color4"/>.</summary>
        public ref readonly Color4 Pixels => ref Unsafe.AsRef<Color4>(_pixels);

        /// <summary>Get pixel of specified (x, y)</summary>
        /// <param name="x">x index (column line)</param>
        /// <param name="y">y index (row line)</param>
        /// <returns>pixel</returns>
        public ref readonly Color4 this[int x, int y]
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

        /// <summary>Get empty <see cref="ReadOnlyRawImageF"/></summary>
        public static ReadOnlyRawImageF Empty => default;

        /// <summary>Create new <see cref="ReadOnlyRawImageF"/></summary>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>
        /// <param name="pixels">pointer to pixels of <see cref="Color4"/></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyRawImageF(int width, int height, IntPtr pixels) : this(width, height, (Color4*)pixels)
        {
        }

        /// <summary>Create new <see cref="ReadOnlyRawImageF"/></summary>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>
        /// <param name="pixels">pointer to pixels</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyRawImageF(int width, int height, Color4* pixels)
        {
            if(width < 0) {
                ThrowOutOfRange(nameof(width));
            }
            if(height < 0) {
                ThrowOutOfRange(nameof(height));
            }
            Width = width;
            Height = height;
            _pixels = pixels;
        }

        public Color4* GetPtr() => _pixels;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<Color4> AsSpan()
        {
            return MemoryMarshal.CreateSpan(ref Unsafe.AsRef<Color4>(_pixels), Width * Height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<Color4> GetRowLine(int row)
        {
            if((uint)row >= (uint)Height) {
                ThrowOutOfRange(nameof(row));
            }
            return MemoryMarshal.CreateSpan(ref Unsafe.AsRef<Color4>(_pixels + row * Width), Width);
        }

        public override bool Equals(object? obj) => obj is ReadOnlyRawImageF f && Equals(f);

        public bool Equals(ReadOnlyRawImageF other) => Width == other.Width && Height == other.Height && _pixels == other._pixels;

        public override int GetHashCode() => HashCode.Combine(Width, Height, (IntPtr)_pixels);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlyRawImageF(RawImageF image) => Unsafe.As<RawImageF, ReadOnlyRawImageF>(ref image);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in ReadOnlyRawImageF left, in ReadOnlyRawImageF right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in ReadOnlyRawImageF left, in ReadOnlyRawImageF right) => !(left == right);

        [DoesNotReturn]
        private static void ThrowOutOfRange(string message) => throw new ArgumentOutOfRangeException(message);
    }
}
