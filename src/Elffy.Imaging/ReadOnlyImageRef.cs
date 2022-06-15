#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Elffy.Effective;
using Elffy.Effective.Unsafes;
using Elffy.Imaging.Internal;

namespace Elffy.Imaging
{
    /// <summary>Provides read-only view of image-like object.</summary>
    /// <remarks>
    /// The layout of pixels is (R8, G8, B8, A8) which is similar to <see cref="ColorByte"/>.
    /// </remarks>
    [DebuggerDisplay("{DebugView,nq}")]
    public unsafe readonly ref struct ReadOnlyImageRef
    {
        private readonly ReadOnlySpan<ColorByte> _firstRowLine;     // (ref ColorByte head, int width)
        private readonly int _height;

        /// <summary>Get width of the image.</summary>
        public int Width => _firstRowLine.Length;
        /// <summary>Get height of the image.</summary>
        public int Height => _height;

        /// <summary>Get size of the image</summary>
        public Vector2i Size => new Vector2i(Width, Height);

        /// <summary>Get whether the image is empty or not.</summary>
        public bool IsEmpty
        {
            // A valid empty instance has (width, height) == (0, 0).
            // So I use '||' operator because it's faster than '&&'.

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _firstRowLine.Length == 0 || _height == 0;
        }

        /// <summary>Get an empty instance of type <see cref="ReadOnlyImageRef"/>.</summary>
        public static ReadOnlyImageRef Empty => default;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebugView => $"{nameof(ReadOnlyImageRef)} ({Width}x{Height})";

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
                    ThrowHelper.ThrowArgOutOfRange(nameof(x));
                }
                if((uint)y >= (uint)Height) {
                    ThrowHelper.ThrowArgOutOfRange(nameof(y));
                }
                return ref _firstRowLine.At(y * Width + x);
            }
        }

        /// <summary>Create <see cref="ReadOnlyImageRef"/> from a pointer, width, and height.</summary>
        /// <param name="pixels">pointer of the head pixel</param>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyImageRef(ColorByte* pixels, int width, int height)
        {
            if(width <= 0) {
                if(width == 0) {
                    this = default;
                    return;
                }
                else {
                    ThrowHelper.ThrowArgOutOfRange(nameof(width));
                }
            }
            if(height <= 0) {
                if(height == 0) {
                    this = default;
                    return;
                }
                else {
                    ThrowHelper.ThrowArgOutOfRange(nameof(height));
                }
            }
            _firstRowLine = MemoryMarshal.CreateSpan(ref *pixels, width);
            _height = height;
        }

        /// <summary>Create <see cref="ReadOnlyImageRef"/> from a reference, width, and height.</summary>
        /// <param name="pixels">reference to the head pixel</param>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyImageRef(ref ColorByte pixels, int width, int height)
        {
            if(width <= 0) {
                if(width == 0) {
                    this = default;
                    return;
                }
                else {
                    ThrowHelper.ThrowArgOutOfRange(nameof(width));
                }
            }
            if(height <= 0) {
                if(height == 0) {
                    this = default;
                    return;
                }
                else {
                    ThrowHelper.ThrowArgOutOfRange(nameof(height));
                }
            }
            _firstRowLine = MemoryMarshal.CreateSpan(ref pixels, width);
            _height = height;
        }

        /// <summary>Create <see cref="ReadOnlyImageRef"/> from whole pixels data, width, and height.</summary>
        /// <param name="pixels">whole pixels data</param>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyImageRef(ReadOnlySpan<ColorByte> pixels, int width, int height)
        {
            if(width < 0) {
                ThrowHelper.ThrowArgOutOfRange(nameof(width));
            }
            if(height < 0) {
                ThrowHelper.ThrowArgOutOfRange(nameof(height));
            }
            if(pixels.Length != width * height) {
                ThrowHelper.ThrowArgException($"Length of {nameof(pixels)} must be width * height.");
            }
            _firstRowLine = pixels.SliceUnsafe(0, width);
            _height = height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlyImageRef(ReadOnlySpan<ColorByte> firstRowLine, int height)
        {
            // Create an instance without any check.
            Debug.Assert(
                (firstRowLine.Length > 0 && height > 0) ||      // not empty image
                (firstRowLine.Length == 0 && height == 0)       // empty image
                );
            _firstRowLine = firstRowLine;
            _height = height;
        }

        /// <summary>Create <see cref="ReadOnlyImageRef"/> without any check.</summary>
        /// <remarks>[Caution] Arguments are not checked.</remarks>
        /// <param name="firstRowLine">pixels span of first row line</param>
        /// <param name="height">height of image</param>
        /// <returns><see cref="ReadOnlyImageRef"/> instance</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlyImageRef CreateUnsafe(ReadOnlySpan<ColorByte> firstRowLine, int height)
        {
            return new(firstRowLine, height);
        }

        /// <summary>Get pixels span</summary>
        /// <returns>pixels span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<ColorByte> GetPixels()
        {
            return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(_firstRowLine.GetReference()), _firstRowLine.Length * _height);
        }

        /// <summary>Get span of the specified row line pixels.</summary>
        /// <param name="row">row index</param>
        /// <returns>row line span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<ColorByte> GetRowLine(int row)
        {
            if((uint)row >= _height) {
                ThrowHelper.ThrowArgOutOfRange(nameof(row));
            }
            return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(_firstRowLine.At(row * Width)), Width);
        }

        /// <summary>Create copy image</summary>
        /// <returns>new <see cref="Image"/> copied from <see langword="this"/></returns>
        public Image ToImage()
        {
            var image = new Image(Width, Height, false);
            try {
                GetPixels().CopyTo(image.GetPixels());
                return image;
            }
            catch {
                image.Dispose();
                throw;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref readonly ColorByte GetReference()
        {
            return ref _firstRowLine.GetReference();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref readonly ColorByte GetPinnableReference()
        {
            return ref _firstRowLine.GetPinnableReference();
        }

        public override string ToString() => DebugView;

#pragma warning disable 0809
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Equals() will always throw an exception.")]
        public override bool Equals(object? obj) => throw new NotSupportedException("Equals() will always throw an exception.");

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("GetHashCode() will always throw an exception.")]
        public override int GetHashCode() => throw new NotSupportedException("GetHashCode() will always throw an exception.");
#pragma warning restore 0809

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(ReadOnlyImageRef left, ReadOnlyImageRef right)
        {
            return left._firstRowLine == right._firstRowLine && left._height == right._height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(ReadOnlyImageRef left, ReadOnlyImageRef right)
        {
            return !(left == right);
        }
    }
}
