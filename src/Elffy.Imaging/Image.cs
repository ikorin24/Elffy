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
    public unsafe readonly struct Image : IEquatable<Image>, IDisposable
    {
        private const string Message_EmptyOrDisposed = "The image is empty or already disposed.";

        private readonly IImageSource? _source;
        private readonly short _token;

        public int Width => _source?.Width ?? 0;
        public int Height => _source?.Height ?? 0;

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

        public static Image FromStream(Stream stream, string fileExtension)
        {
            return FromStream(stream, GetTypeFromExt(fileExtension));
        }

        public static Image FromStream(Stream stream, ReadOnlySpan<char> fileExtension)
        {
            return FromStream(stream, GetTypeFromExt(fileExtension));
        }

        public static Image FromStream(Stream stream, ImageType type)
        {
            var source = LoadToImageSource(stream, type);
            return new Image(source, source.Token);
        }

        public static IImageSource LoadToImageSource(Stream stream, ImageType type)
        {
            if(type is ImageType.Png or ImageType.Jpg or ImageType.Bmp) {
                return ParseToImageSouce(stream);
            }
            else if(type == ImageType.Tga) {
                return TgaParser.ParseToImageSource(stream);
            }
            else {
                throw new NotSupportedException($"Not supported type : {type}");
            }
        }

        public static ImageType GetTypeFromExt(ReadOnlySpan<char> ext)
        {
            static bool StringEquals(ReadOnlySpan<char> left, string right)
                => left.Equals(right.AsSpan(), StringComparison.OrdinalIgnoreCase);

            if(StringEquals(ext, ".png")) {
                return ImageType.Png;
            }
            else if(StringEquals(ext, ".jpg") || StringEquals(ext, ".jpeg")) {
                return ImageType.Jpg;
            }
            else if(StringEquals(ext, ".tga")) {
                return ImageType.Tga;
            }
            else if(StringEquals(ext, ".bmp")) {
                return ImageType.Bmp;
            }
            else {
                throw new NotSupportedException($"Not supported extension. {ext.ToString()}");
            }
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

        private static Image ParseStreamToImage(Stream stream)
        {
            using var skBitmap = ParseToSKBitmap(stream);
            if(skBitmap.ColorType != SKColorType.Rgba8888) {
                throw new NotSupportedException();
            }
            var image = new Image(skBitmap.Width, skBitmap.Height, false);
            try {
                skBitmap.GetPixelSpan()
                      .MarshalCast<byte, ColorByte>()
                      .CopyTo(image.GetPixels());
                return image;
            }
            catch {
                image.Dispose();
                throw;
            }

            static SKBitmap ParseToSKBitmap(Stream stream)
            {
                using var buf = stream.ReadToEnd(out var len);
                using var skData = SKData.Create(buf.Ptr, len);
                using var codec = SKCodec.Create(skData);
                var info = codec.Info;
                info.ColorType = SKColorType.Rgba8888;
                info.AlphaType = SKAlphaType.Unpremul;
                var skBitmap = SKBitmap.Decode(codec, info);
                Debug.Assert(skBitmap.ColorType == SKColorType.Rgba8888);
                return skBitmap;
            }
        }

        private static IImageSource ParseToImageSouce(Stream stream)
        {
            var skBitmap = ParseToSKBitmap(stream);
            try {
                return new SKBitmapImageSouce(skBitmap);
            }
            catch {
                skBitmap.Dispose();
                throw;
            }

            static SKBitmap ParseToSKBitmap(Stream stream)
            {
                using var buf = stream.ReadToEnd(out var len);
                using var skData = SKData.Create(buf.Ptr, len);
                using var codec = SKCodec.Create(skData);
                var info = codec.Info;
                info.ColorType = SKColorType.Rgba8888;
                info.AlphaType = SKAlphaType.Unpremul;
                var skBitmap = SKBitmap.Decode(codec, info);
                Debug.Assert(skBitmap.ColorType == SKColorType.Rgba8888);
                return skBitmap;
            }
        }

        private unsafe sealed class SKBitmapImageSouce : IImageSource
        {
            private SKBitmap? _skBitmap;

            public int Width => _skBitmap?.Width ?? 0;
            public int Height => _skBitmap?.Height ?? 0;
            public ColorByte* Pixels => (ColorByte*)(_skBitmap?.GetPixels() ?? IntPtr.Zero);
            public short Token => 0;

            public SKBitmapImageSouce(SKBitmap skBitmap)
            {
                if(skBitmap is null) { ThrowHelper.ThrowNullArg(nameof(skBitmap)); }
                if(skBitmap.ColorType != SKColorType.Rgba8888) { throw new NotSupportedException("ColorType must be Rgba8888."); }
                _skBitmap = skBitmap;
            }

            public void Dispose()
            {
                _skBitmap?.Dispose();
                _skBitmap = null;
            }

            public Span<ColorByte> GetPixels()
            {
                var skBitmap = _skBitmap;
                if(skBitmap == null) {
                    return Span<ColorByte>.Empty;
                }
                return new Span<ColorByte>((void*)skBitmap.GetPixels(), skBitmap.Width * skBitmap.Height);
            }
        }
    }
}
