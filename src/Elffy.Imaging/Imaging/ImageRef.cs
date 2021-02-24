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
    [DebuggerDisplay("{DebugView,nq}")]
    public unsafe readonly ref struct ImageRef
    {
        private readonly Span<ColorByte> _firstRowLine;     // (ref ColorByte head, int width)
        private readonly int _height;

        public int Width => _firstRowLine.Length;
        public int Height => _height;

        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _firstRowLine.Length == 0 && _height == 0;
        }

        public static ImageRef Empty => default;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebugView => $"{nameof(ImageRef)} ({Width}x{Height})";

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
                    ThrowHelper.ThrowArgOutOfRange(nameof(x));
                }
                if((uint)y >= (uint)Height) {
                    ThrowHelper.ThrowArgOutOfRange(nameof(y));
                }
                return ref _firstRowLine.At(y * Width + x);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ImageRef(ColorByte* pixels, int width, int height)
        {
            if(width <= 0) {
                ThrowHelper.ThrowArgOutOfRange(nameof(width));
            }
            if(height <= 0) {
                ThrowHelper.ThrowArgOutOfRange(nameof(height));
            }
            _firstRowLine = MemoryMarshal.CreateSpan(ref *pixels, width);
            _height = height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ImageRef(ref ColorByte pixels, int width, int height)
        {
            if(width <= 0) {
                ThrowHelper.ThrowArgOutOfRange(nameof(width));
            }
            if(height <= 0) {
                ThrowHelper.ThrowArgOutOfRange(nameof(height));
            }
            _firstRowLine = MemoryMarshal.CreateSpan(ref pixels, width);
            _height = height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ImageRef(ref ColorByte pixels, int width, int height, int dummyArg)
        {
            // dummyArg is not used.
            // This constructor does not check width and height.
            Debug.Assert(width > 0 && height > 0);
            _firstRowLine = MemoryMarshal.CreateSpan(ref pixels, width);
            _height = height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<ColorByte> GetPixels()
        {
            return MemoryMarshal.CreateSpan(ref _firstRowLine.GetReference(), _firstRowLine.Length * _height);
        }

        /// <summary>Get span of the specified row line pixels.</summary>
        /// <param name="row">row index</param>
        /// <returns>row line span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<ColorByte> GetRowLine(int row)
        {
            if((uint)row >= _height) {
                ThrowHelper.ThrowArgOutOfRange(nameof(row));
            }
            return MemoryMarshal.CreateSpan(ref _firstRowLine.At(row * Width), Width);
        }

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
        public ref ColorByte GetReference()
        {
            return ref _firstRowLine.GetReference();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ref ColorByte GetPinnableReference()
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
        public static bool operator ==(ImageRef left, ImageRef right)
        {
            return left._firstRowLine == right._firstRowLine && left._height == right._height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(ImageRef left, ImageRef right)
        {
            return !(left == right);
        }
    }
}
