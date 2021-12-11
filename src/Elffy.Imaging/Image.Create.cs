#nullable enable
using System;
using System.IO;
using System.Diagnostics;
using Elffy.Imaging.Internal;
using Elffy.Effective;
using SkiaSharp;

namespace Elffy.Imaging
{
    partial struct Image
    {
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
