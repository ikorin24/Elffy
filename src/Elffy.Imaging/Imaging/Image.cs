#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.IO;
using Elffy.Imaging.Internal;

namespace Elffy.Imaging
{
    public unsafe readonly struct Image : IEquatable<Image>, IDisposable
    {
        private readonly ImageObj? _image;
        private readonly uint _token;

        public int Width => _image?.Width ?? 0;
        public int Height => _image?.Height ?? 0;

        /// <summary>Get pointer to pixels of type <see cref="ColorByte"/></summary>
        public IntPtr Ptr => _image is not null ? (IntPtr)_image.Pixels : IntPtr.Zero;

        public bool IsEmpty => _token == 0;

        public static Image Empty => default;

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
                if(_image is null || _image.Token != _token) {
                    ThrowHelper.ThrowInvalidOp("The image is empty or already disposed.");
                }
                return ref _image.Pixels[y * Width + x];
            }
        }

        public Image(int width, int height)
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
                _image = new ImageObj(width, height);
                _token = _image.Token;
            }
        }

        public void Dispose()
        {
            var image = Interlocked.Exchange(ref Unsafe.AsRef(in _image), null);
            image?.Dispose();
            Unsafe.AsRef(_token) = 0;
        }

        public ColorByte* GetPtr() => _image is not null ? _image.Pixels : null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<ColorByte> GetPixels()
        {
            if(_image is null) {
                return Span<ColorByte>.Empty;
            }
            else {
                return MemoryMarshal.CreateSpan(ref *_image.Pixels, Width * Height);
            }
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
                // TODO: bmp, jpg
                ImageType.Bmp or ImageType.Jpg or _ => throw new NotSupportedException($"Not supported type : {type}"),
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

        internal unsafe sealed class ImageObj : IDisposable
        {
            private int _width;
            private int _height;
            private ColorByte* _pixels;
            private uint _token;

            public int Width => _width;
            public int Height => _height;
            public ColorByte* Pixels => _pixels;
            public uint Token => _token;

            public ImageObj(int width, int height)
            {
                _width = width;
                _height = height;
                var len = width * height;
                _pixels = (ColorByte*)Marshal.AllocHGlobal(sizeof(ColorByte) * len);
                new Span<ColorByte>(_pixels, len).Clear();
                _token = 1;
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
        }
    }
}
