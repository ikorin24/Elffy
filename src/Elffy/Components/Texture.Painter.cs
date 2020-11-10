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

            public void DrawText(string text, SKFont font, in Vector2 pos, in ColorByte color, bool flush = false)
            {
                DrawText(text.AsSpan(), font, pos, color, flush);
            }

            public void DrawText(ReadOnlySpan<char> text, SKFont font, in Vector2 pos, in ColorByte color, bool flush = false)
            {
                if(font is null) {
                    ThrowNullArg();
                    static void ThrowNullArg() => throw new ArgumentNullException(nameof(font));
                }

                DrawTextCore(text.MarshalCast<char, byte>(), SKTextEncoding.Utf16, font!, pos, GetSKColor(color));
                if(flush) {
                    Flush();
                }
            }

            public void DrawText(ReadOnlySpan<byte> utf8Text, SKFont font, in Vector2 pos, in ColorByte color, bool flush = false)
            {
                if(font is null) {
                    ThrowNullArg();
                    static void ThrowNullArg() => throw new ArgumentNullException(nameof(font));
                }

                DrawTextCore(utf8Text, SKTextEncoding.Utf8, font!, pos, GetSKColor(color));
                if(flush) {
                    Flush();
                }
            }

            private void DrawTextCore(ReadOnlySpan<byte> text, SKTextEncoding enc, SKFont font, in Vector2 pos, SKColor color)
            {
                var glyphCount = font.CountGlyphs(text, enc);
                if(glyphCount == 0) {
                    return;
                }

                var builder = TextBuilder;

                var buffer = builder.AllocatePositionedRun(font, glyphCount);
                var glyphs = buffer.GetGlyphSpan();
                font.GetGlyphs(text, enc, glyphs);
                font.GetGlyphPositions(glyphs, buffer.GetPositionSpan());
                //var width = font.MeasureText(glyphs, out _);
                //var fontMetrics = font.Metrics;
                //var changedSize = new Vector2i((int)width + 1, (int)(fontMetrics.Descent - fontMetrics.Ascent) + 1);

                var paint = Paint;

                paint.Reset();
                paint.Color = color;
                paint.Style = SKPaintStyle.Fill;
                paint.IsAntialias = false;

                var canvas = Canvas;

                using(var textBlob = builder.Build()) {
                    canvas.DrawText(textBlob, pos.X, pos.Y, _paint);
                }

                SetDirty();
            }

            public void SetDirty()
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
