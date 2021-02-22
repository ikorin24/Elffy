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

namespace Elffy.Imaging
{
    [DebuggerDisplay("{DebugView,nq}")]
    public unsafe readonly struct Image : IEquatable<Image>, IDisposable
    {
        private const string Message_EmptyOrDisposed = "The image is empty or already disposed.";

        private readonly ImageObj? _image;
        private readonly uint _token;

        public int Width => _image?.Width ?? 0;
        public int Height => _image?.Height ?? 0;

        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _image is null || _image.Token != _token;
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
                var image = _image;
                if(image is null) {
                    ThrowHelper.ThrowInvalidOp(Message_EmptyOrDisposed);
                }
                if((uint)x >= (uint)image.Width) {
                    ThrowHelper.ThrowArgOutOfRange(nameof(x));
                }
                if((uint)y >= (uint)image.Height) {
                    ThrowHelper.ThrowArgOutOfRange(nameof(y));
                }
                return ref image.Pixels[y * Width + x];
            }
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
                // TODO: instance pooling
                _image = new ImageObj(width, height, zeroFill);
                _token = _image.Token;
            }
        }

        public override string ToString() => DebugView;

        public void Dispose()
        {
            var image = Interlocked.Exchange(ref Unsafe.AsRef(in _image), null);
            image?.Dispose();
            Unsafe.AsRef(_token) = 0;
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
            Debug.Assert(_image is not null);
            return _image.Pixels;
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
            Debug.Assert(_image is not null);
            return MemoryMarshal.CreateSpan(ref *_image.Pixels, Width * Height);
        }

        /// <summary>Get span of the specified row line pixels.</summary>
        /// <param name="row">row index</param>
        /// <returns>row line span</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Span<ColorByte> GetRowLine(int row)
        {
            var image = _image;
            if(image is null || image.Token != _token) {
                ThrowHelper.ThrowInvalidOp(Message_EmptyOrDisposed);
            }
            if((uint)row >= (uint)image.Height) {
                ThrowHelper.ThrowArgOutOfRange(nameof(row));
            }
            return MemoryMarshal.CreateSpan(ref *(image.Pixels + image.Width * row), image.Width);
        }

        /// <summary>Create deep copy of the image</summary>
        /// <returns></returns>
        public Image Clone()
        {
            var image = _image;
            if(image is null || image.Token != _token) {
                return Empty;
            }
            var clone = new Image(image.Width, image.Height);
            Debug.Assert(clone._image is not null);
            var source = MemoryMarshal.CreateSpan(ref *image.Pixels, image.Width * image.Height);
            var dest = MemoryMarshal.CreateSpan(ref *clone._image.Pixels, clone._image.Width * clone._image.Height);
            source.CopyTo(dest);
            return clone;
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
            return type switch
            {
                ImageType.Png => PngParser.Parse(stream),
                ImageType.Tga => TgaParser.Parse(stream),
                ImageType.Jpg => ImageParserTemporary.Parse(stream),    // TODO: jpg parser
                ImageType.Bmp => ImageParserTemporary.Parse(stream),    // TODO: bmp parser
                _ => throw new NotSupportedException($"Not supported type : {type}"),
            };
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

        public bool Equals(Image other) => _image == other._image && _token == other._token;

        public override int GetHashCode() => HashCode.Combine(_image, _token);

        public static bool operator ==(Image left, Image right) => left.Equals(right);

        public static bool operator !=(Image left, Image right) => !(left == right);

        [DebuggerDisplay("{DebugView,nq}")]
        private unsafe sealed class ImageObj : IDisposable
        {
            private static uint _tokenFactory;

            private int _width;
            private int _height;
            private ColorByte* _pixels;
            private uint _token;

            public int Width => _width;
            public int Height => _height;
            public ColorByte* Pixels => _pixels;
            public uint Token => _token;

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private string DebugView => $"(Token = {_token})";

            public ImageObj(int width, int height, bool zeroFill)
            {
                _width = width;
                _height = height;
                var len = width * height;
                _pixels = (ColorByte*)Marshal.AllocHGlobal(sizeof(ColorByte) * len);
                if(zeroFill) {
                    new Span<ColorByte>(_pixels, len).Clear();
                }

                do {
                    _token = (uint)Interlocked.Increment(ref Unsafe.As<uint, int>(ref _tokenFactory));
                }
                while(_token == 0);
            }

            ~ImageObj() => Dispose(false);

            public void Dispose()
            {
                GC.SuppressFinalize(this);
                Dispose(true);
            }

            private void Dispose(bool disposing)
            {
                Marshal.FreeHGlobal((IntPtr)_pixels);
                _pixels = null;
                _width = 0;
                _height = 0;
                _token = 0;
            }

            public override string ToString() => DebugView;
        }
    }
}

namespace Elffy.Imaging.Internal
{
    internal unsafe static class ImageParserTemporary
    {
        // I want to parse 'jpg' and 'bmp' by my own pure C# parser in the future.

        public static Image Parse(Stream stream)
        {
            using var buf = stream.ReadToEnd(out var len);
            using var skBitmap = SKBitmap.Decode(buf.AsSpan(0, len));
            if(skBitmap.ColorType != SKColorType.Rgba8888) {
                using var tmp = skBitmap.Copy(SKColorType.Rgba8888);
                return CreateImage(tmp);
            }
            else {
                return CreateImage(skBitmap);
            }

            static Image CreateImage(SKBitmap bitmap)
            {
                var image = new Image(bitmap.Width, bitmap.Height, false);
                try {
                    bitmap.GetPixelSpan()
                          .MarshalCast<byte, ColorByte>()
                          .CopyTo(image.GetPixels());
                    return image;
                }
                catch {
                    image.Dispose();
                    throw;
                }
            }
        }
    }
}
