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

            var info = new SKImageInfo
            {
                AlphaType = SKAlphaType.Unpremul,
                Width = baseImage.Width,
                Height = baseImage.Height,
                ColorSpace = null,
                ColorType = SKColorType.Rgba8888,
            };
            using var skImage = SKImage.FromPixels(info, (IntPtr)baseImage.Pixels);
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
}
