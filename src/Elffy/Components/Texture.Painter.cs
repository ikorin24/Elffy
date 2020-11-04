#nullable enable
using System;
using Elffy.Effective;
using SkiaSharp;

namespace Elffy.Components
{
    partial class Texture
    {
        unsafe partial struct Painter
        {
            public void Fill(in ColorByte color, bool flush = false)
            {
                var canvas = Canvas;
                canvas.Clear(GetSKColor(color));
                SetDirty();
                if(flush) {
                    Flush();
                }
            }

            public void DrawText(string text, SKFont font, in ColorByte color, bool flush = false)
            {
                if(text is null) {
                    ThrowNullArg();
                    static void ThrowNullArg() => throw new ArgumentNullException(nameof(text));
                }
                DrawText(text.AsSpan(), font, color, flush);
            }

            public void DrawText(ReadOnlySpan<char> text, SKFont font, in ColorByte color, bool flush = false)
            {
                if(font is null) {
                    ThrowNullArg();
                    static void ThrowNullArg() => throw new ArgumentNullException(nameof(font));
                }

                DrawTextCore(text.MarshalCast<char, byte>(), SKTextEncoding.Utf16, font!, GetSKColor(color), out _);
                if(flush) {
                    Flush();
                }
            }

            public void DrawText(ReadOnlySpan<byte> utf8Text, SKFont font, in ColorByte color, bool flush = false)
            {
                if(font is null) {
                    ThrowNullArg();
                    static void ThrowNullArg() => throw new ArgumentNullException(nameof(font));
                }

                DrawTextCore(utf8Text, SKTextEncoding.Utf8, font!, GetSKColor(color), out _);
                if(flush) {
                    Flush();
                }
            }

            private void DrawTextCore(ReadOnlySpan<byte> text, SKTextEncoding enc, SKFont font, SKColor color, out Vector2i changedSize)
            {
                var glyphCount = font.CountGlyphs(text, enc);
                if(glyphCount == 0) {
                    changedSize = default;
                    return;
                }

                var builder = TextBuilder;

                var buffer = builder.AllocatePositionedRun(font, glyphCount);
                var glyphs = buffer.GetGlyphSpan();
                font.GetGlyphs(text, enc, glyphs);
                font.GetGlyphPositions(glyphs, buffer.GetPositionSpan());
                var width = font.MeasureText(glyphs, out _);
                var fontMetrics = font.Metrics;

                changedSize = new Vector2i((int)width + 1, (int)(fontMetrics.Descent - fontMetrics.Ascent) + 1);

                var paint = Paint;

                paint.Reset();
                paint.Color = color;
                paint.StrokeWidth = 1f;
                paint.Style = SKPaintStyle.Stroke;
                paint.IsAntialias = true;

                var canvas = Canvas;

                using(var textBlob = builder.Build()) {
                    canvas.DrawText(textBlob, 0, -fontMetrics.Ascent, _paint);
                }

                SetDirty();
            }

            private void SetDirty()
            {
                _isDirty = true;
                if(_pixels == null) {
                    _pixels = (byte*)Bitmap.GetPixels();
                }
            }

            private static SKColor GetSKColor(in ColorByte color)
            {
                // Don't use 'Unsafe.As'.
                return new SKColor(color.R, color.G, color.B, color.A);
            }
        }
    }
}
