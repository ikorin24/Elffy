#nullable enable
using System;
using SkiaSharp;

namespace Elffy.Imaging
{
    unsafe partial struct Image
    {
        public Image Resized(Vector2i size)
        {
            var source = _source;
            if(source == null) {
                throw new InvalidOperationException();
            }
            return Resized(source, size);
        }

        public static Image Resized(IImageSource baseImage, Vector2i size)
        {
            if(baseImage == null) { throw new ArgumentNullException(nameof(baseImage)); }
            return Resized(baseImage.AsImageRef(), size);
        }

        public static Image Resized(ReadOnlyImageRef image, Vector2i size)
        {
            var info = new SKImageInfo
            {
                AlphaType = SKAlphaType.Unpremul,
                Width = image.Width,
                Height = image.Height,
                ColorSpace = null,
                ColorType = SKColorType.Rgba8888,
            };
            var pixels = image.GetPixels();
            fixed(ColorByte* p = pixels) {
                using var skImage = SKImage.FromPixels(info, (IntPtr)p);
                using var skBitmap = SKBitmap.FromImage(skImage);

                var resizedInfo = new SKImageInfo
                {
                    AlphaType = SKAlphaType.Unpremul,
                    Width = size.X,
                    Height = size.Y,
                    ColorSpace = null,
                    ColorType = SKColorType.Rgba8888,
                };
                var resized = skBitmap.Resize(resizedInfo, SKFilterQuality.High);
                try {
                    return new Image(new SKBitmapImageSouce(resized), 0);
                }
                catch {
                    resized.Dispose();
                    throw;
                }
            }
        }

        public void ResizeTo(ImageRef dest) => Resize(this, dest);

        public static void Resize(ReadOnlyImageRef source, ImageRef dest)
        {
            const int BytePerPix = 4;

            fixed(ColorByte* sp = source.GetPixels())
            fixed(ColorByte* dp = dest.GetPixels()) {
                var sourceInfo = new SKImageInfo
                {
                    AlphaType = SKAlphaType.Unpremul,
                    Width = source.Width,
                    Height = source.Height,
                    ColorSpace = null,
                    ColorType = SKColorType.Rgba8888,
                };
                var destInfo = new SKImageInfo
                {
                    AlphaType = SKAlphaType.Unpremul,
                    Width = dest.Width,
                    Height = dest.Height,
                    ColorSpace = null,
                    ColorType = SKColorType.Rgba8888,
                };
                using var sourceImage = new SKPixmap(sourceInfo, (IntPtr)sp, source.Width * BytePerPix);
                using var destImage = new SKPixmap(destInfo, (IntPtr)dp, dest.Width * BytePerPix);
                sourceImage.ScalePixels(destImage, SKFilterQuality.High);
            }
        }
    }
}
