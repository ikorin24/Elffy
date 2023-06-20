#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using System.Diagnostics;
using Elffy.Imaging.Internal;
using Elffy.Effective;
using SkiaSharp;
using System.ComponentModel;

namespace Elffy.Imaging
{
    [DebuggerDisplay("{DebugView,nq}")]
    public unsafe readonly partial struct Image : IEquatable<Image>, IDisposable
    {
        private const string Message_EmptyOrDisposed = "The image is empty or already disposed.";

        private readonly IImageSource? _source;
        private readonly short _token;

        public int Width => _source?.Width ?? 0;
        public int Height => _source?.Height ?? 0;

        public Vector2i Size
        {
            get
            {
                var source = _source;
                return (source == null) ? Vector2i.Zero : new Vector2i(source.Width, source.Height);
            }
        }

        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _source is null || _source.Token != _token;
        }

        public static Image Empty => default;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebugView => $"{typeof(Image).FullName} ({Width}x{Height})";

        /// <summary>Get or set pixel of specified (x, y)</summary>
        /// <param name="x">x index (column line)</param>
        /// <param name="y">y index (row line)</param>
        /// <returns>pixel</returns>
        public ref ColorByte this[int x, int y]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var source = _source;
                if(source is null) {
                    ThrowHelper.ThrowInvalidOp(Message_EmptyOrDisposed);
                }
                if((uint)x >= (uint)source.Width) {
                    ThrowHelper.ThrowArgOutOfRange(nameof(x));
                }
                if((uint)y >= (uint)source.Height) {
                    ThrowHelper.ThrowArgOutOfRange(nameof(y));
                }
                return ref source.Pixels[y * Width + x];
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Don't use default constructor.", true)]
        public Image()
        {
            throw new NotSupportedException("Don't use default constructor.");
        }

        /// <summary>Create a new image with specified size, which pixels are initialized as (0, 0, 0, 0).</summary>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>
        public Image(int width, int height) : this(width, height, true)
        {
        }

        public Image(uint width, uint height) : this(checked((int)width), checked((int)height))
        {
        }

        /// <summary>Create a new image with specified size.</summary>
        /// <remarks>If <paramref name="zeroFill"/> is false, the pixels are not initialized. You must set them before using the image.</remarks>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>
        /// <param name="zeroFill">initializing all pixels as (0, 0, 0, 0) or not.</param>
        public Image(int width, int height, bool zeroFill)
        {
            if(width < 0) {
                ThrowHelper.ThrowArgOutOfRange(nameof(width));
            }
            if(height < 0) {
                ThrowHelper.ThrowArgOutOfRange(nameof(height));
            }

            if(width == 0 || height == 0) {
                this = default;
            }
            else {
                this = DefaultImageSource.CreateImage(width, height, zeroFill);
            }
        }

        public Image(uint width, uint height, bool zeroFill) : this(checked((int)width), checked((int)height), zeroFill)
        {
        }

        /// <summary>Create a new image with specified size.</summary>
        /// <param name="width">image width</param>
        /// <param name="height">image height</param>
        /// <param name="fill">color to initialize all pixels</param>
        public Image(int width, int height, ColorByte fill) : this(width, height, false)
        {
            GetPixels().Fill(fill);
        }

        public Image(uint width, uint height, ColorByte fill) : this(checked((int)width), checked((int)height), fill)
        {
        }

        public Image(IImageSource source, short token)
        {
            if(source == null) {
                ThrowHelper.ThrowNullArg(nameof(source));
            }
            _source = source;
            _token = token;
        }

        public override string ToString() => DebugView;

        public void Dispose()
        {
            var source = Interlocked.Exchange(ref Unsafe.AsRef(in _source), null);
            Unsafe.AsRef(_token) = 0;
            source?.Dispose();
        }

        /// <summary>Get pointer to the pixels.</summary>
        /// <remarks>Throws <see cref="InvalidOperationException"/> if <see cref="IsEmpty"/> == <see langword="true"/>.</remarks>
        /// <exception cref="InvalidOperationException">The image is empty</exception>
        /// <returns>pointer to the pixels</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ColorByte* GetPtr()
        {
            if(IsEmpty) {
                ThrowHelper.ThrowInvalidOp(Message_EmptyOrDisposed);
            }
            Debug.Assert(_source is not null);
            return _source.Pixels;
        }

        /// <summary>Get span of the pixels.</summary>
        /// <remarks>Returns empty span if <see cref="IsEmpty"/> == <see langword="true"/></remarks>
        /// <returns>span of the pixels</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<ColorByte> GetPixels()
        {
            if(IsEmpty) {
                return Span<ColorByte>.Empty;
            }
            Debug.Assert(_source is not null);
            return MemoryMarshal.CreateSpan(ref *_source.Pixels, Width * Height);
        }

        /// <summary>Get span of the specified row line pixels.</summary>
        /// <param name="row">row index</param>
        /// <returns>row line span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Span<ColorByte> GetRowLine(int row)
        {
            var source = _source;
            if(source is null || source.Token != _token) {
                ThrowHelper.ThrowInvalidOp(Message_EmptyOrDisposed);
            }
            if((uint)row >= (uint)source.Height) {
                ThrowHelper.ThrowArgOutOfRange(nameof(row));
            }
            return MemoryMarshal.CreateSpan(ref *(source.Pixels + source.Width * row), source.Width);
        }

        /// <summary>Create deep copy of the image</summary>
        /// <returns>clone image</returns>
        public Image ToImage()
        {
            var source = _source;
            if(source is null || source.Token != _token) {
                return Empty;
            }
            var clone = new Image(source.Width, source.Height, false);
            try {
                Debug.Assert(clone._source is not null);
                var sourceSpan = MemoryMarshal.CreateSpan(ref *source.Pixels, source.Width * source.Height);
                var destSpan = MemoryMarshal.CreateSpan(ref *clone._source.Pixels, clone._source.Width * clone._source.Height);
                sourceSpan.CopyTo(destSpan);
                return clone;
            }
            catch {
                clone.Dispose();
                throw;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ImageRef AsImageRef()
        {
            var source = _source;
            if(source is null || source.Token != _token) {
                return ImageRef.Empty;
            }
            Debug.Assert(source.Height > 0 && source.Height > 0);
            var firstRowLine = MemoryMarshal.CreateSpan(ref *source.Pixels, source.Width);
            return ImageRef.CreateUnsafe(firstRowLine, source.Height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyImageRef AsReadOnlyImageRef()
        {
            var source = _source;
            if(source is null || source.Token != _token) {
                return ImageRef.Empty;
            }
            Debug.Assert(source.Height > 0 && source.Height > 0);
            var firstRowLine = MemoryMarshal.CreateSpan(ref *source.Pixels, source.Width);
            return ReadOnlyImageRef.CreateUnsafe(firstRowLine, source.Height);
        }

        public static ImageType GetTypeFromExt(string ext) => GetTypeFromExt(ext.AsSpan());

        public override bool Equals(object? obj) => obj is Image image && Equals(image);

        public bool Equals(Image other) => _source == other._source && _token == other._token;

        public override int GetHashCode() => HashCode.Combine(_source, _token);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Image left, Image right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Image left, Image right) => !(left == right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ImageRef(Image image) => image.AsImageRef();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlyImageRef(Image image) => image.AsReadOnlyImageRef();
    }
}
