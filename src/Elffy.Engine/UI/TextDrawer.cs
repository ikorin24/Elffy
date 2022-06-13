#nullable enable
using Elffy.Imaging;
using Elffy.Effective;
using System;
using SkiaSharp;

namespace Elffy.UI
{
    internal static class TextDrawer
    {
        public static DrawResult Draw(ReadOnlySpan<byte> utf8Text, in TextDrawOptions options)
        {
            return DrawPrivate(utf8Text, SKTextEncoding.Utf8, options);
        }

        public static DrawResult Draw(ReadOnlySpan<char> text, in TextDrawOptions options)
        {
            return DrawPrivate(text.MarshalCast<char, byte>(), SKTextEncoding.Utf16, options);
        }

        private static unsafe DrawResult DrawPrivate(ReadOnlySpan<byte> text, SKTextEncoding enc, in TextDrawOptions options)
        {
            var targetSize = options.TargetSize;
            var skFont = options.Font;

            var glyphCount = skFont.CountGlyphs(text, enc);
            if(glyphCount == 0) {
                return DrawResult.None;
            }

            using var builder = new SKTextBlobBuilder();    // TODO: pool SKTextBlobBuilder instance
            using var paint = new SKPaint();  // TODO: pool
            paint.Reset();
            var foreground = options.Foreground;
            paint.Color = new SKColor(foreground.R, foreground.G, foreground.B, foreground.A);
            paint.Style = SKPaintStyle.Fill;
            paint.IsAntialias = true;
            paint.SubpixelText = true;
            paint.LcdRenderText = true;
            paint.TextAlign = SKTextAlign.Left;

            var buffer = builder.AllocatePositionedRun(skFont, glyphCount);
            var glyphs = buffer.GetGlyphSpan();
            skFont.GetGlyphs(text, enc, glyphs);
            var glyphPositions = buffer.GetPositionSpan();
            skFont.GetGlyphPositions(glyphs, glyphPositions);

            float boundsWidth;
            {
                skFont.MeasureText(glyphs, out var bounds, paint);
                boundsWidth = bounds.Width + 1;
            }
            skFont.GetFontMetrics(out var metrics);

            var pos = new Vector2
            {
                X = options.Alignment switch
                {
                    HorizontalTextAlignment.Center => (int)((targetSize.X - boundsWidth) / 2),
                    HorizontalTextAlignment.Right => targetSize.X - boundsWidth,
                    HorizontalTextAlignment.Left or _ => 0,
                },
                //Y = options.VerticalAlignment switch
                //{
                //    VerticalTextAlignment.Center => targetSize.Y / 2 - metrics.Bottom + (metrics.Bottom - metrics.Top) / 2,
                //    VerticalTextAlignment.Top => -metrics.Top,
                //    VerticalTextAlignment.Bottom => targetSize.Y - metrics.Bottom,
                //    _ => 0,
                //}
                Y = targetSize.Y / 2 - metrics.Bottom + (metrics.Bottom - metrics.Top) / 2,
            };
            float y0 = pos.Y + metrics.Top;

            var posPixel = new Vector2i
            {
                X = (int)MathF.Floor(pos.X),
                Y = (int)MathF.Floor(y0),
            };
            var posSubpixel = new Vector2
            {
                X = pos.X - posPixel.X,
                Y = y0 - posPixel.Y,
            };

            var size = new Vector2i
            {
                X = (int)MathF.Ceiling(posSubpixel.X + boundsWidth),
                Y = (int)MathF.Ceiling(posSubpixel.Y + metrics.Bottom - metrics.Top),
            };

            // This color type is same as texture inner pixel format of opengl, and Elffy.ColorByte
            const SKColorType ColorType = SKColorType.Rgba8888;
            var info = new SKImageInfo(size.X, size.Y, ColorType, SKAlphaType.Premul);
            var bitmap = new SKBitmap(info);
            var canvas = new SKCanvas(bitmap);
            try {
                var result = new ImageRef((ColorByte*)bitmap.GetPixels(), size.X, size.Y);
                result.GetPixels().Fill(options.Background);

                using(var textBlob = builder.Build()) {
                    var x = posSubpixel.X;
                    var y = posSubpixel.Y - metrics.Top;
                    canvas.DrawText(textBlob, x, y, paint);
                }
                return new DrawResult(bitmap, canvas, result, posPixel);
            }
            catch {
                bitmap.Dispose();
                canvas.Dispose();
                throw;
            }
        }

        internal readonly ref struct DrawResult
        {
            private readonly SKBitmap? _bitmap;
            private readonly SKCanvas? _canvas;
            private readonly ReadOnlyImageRef _result;
            private readonly Vector2i _position;

            public static DrawResult None => default;

            public ReadOnlyImageRef Image => _result;
            public Vector2i Position => _position;

            public bool IsNone => _result.IsEmpty;

            [Obsolete("Don't use default constructor.")]
            public DrawResult() => throw new NotSupportedException("Don't use default constructor.");

            public DrawResult(SKBitmap bitmap, SKCanvas canvas, ReadOnlyImageRef result, Vector2i position)
            {
                _bitmap = bitmap;
                _canvas = canvas;
                _result = result;
                _position = position;
            }

            public void Dispose()
            {
                _bitmap?.Dispose();
                _canvas?.Dispose();
            }
        }
    }

    internal readonly struct TextDrawOptions
    {
        public SKFont Font { get; init; }
        public Vector2 TargetSize { get; init; }
        public HorizontalTextAlignment Alignment { get; init; }
        public ColorByte Foreground { get; init; }
        public ColorByte Background { get; init; }
    }
}
