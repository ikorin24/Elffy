#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Elffy.Effective;
using SkiaSharp;

namespace Elffy.Components
{
    public unsafe struct TexturePainter : IDisposable
    {
        // This color type is same as texture inner pixel format of opengl
        const SKColorType ColorType = SKColorType.Rgba8888;

        private readonly RectI _rect;
        private readonly Texture _t;
        private SKPaint? _paint;
        private SKBitmap? _bitmap;
        private SKCanvas? _canvas;
        private SKTextBlobBuilder? _textBuilder;
        private bool _isDirty;
        private ColorByte* _pixels;      // pointer to a head pixel

        private SKPaint Paint => _paint ??= new SKPaint();
        private SKBitmap Bitmap
        {
            get
            {
                if(_bitmap is null) {
                    _bitmap = new SKBitmap(new SKImageInfo(_rect.Width, _rect.Height, ColorType, SKAlphaType.Premul));
                }
                return _bitmap;
            }
        }
        private SKCanvas Canvas => _canvas ??= new SKCanvas(Bitmap);
        private SKTextBlobBuilder TextBuilder => _textBuilder ??= new SKTextBlobBuilder();

        /// <summary>Get pointer to pixels.</summary>
        /// <remarks>If change pixels via pointer, you must call <see cref="SetDirty"/> method after that.</remarks>
        public ColorByte* Ptr => (ColorByte*)Bitmap.GetPixels();

        internal TexturePainter(Texture texture, in RectI rect, bool copyFromOriginal)
        {
            _rect = rect;
            _t = texture;
            _isDirty = false;
            _pixels = default;
            _paint = null;
            _bitmap = null;
            _canvas = null;
            _textBuilder = null;
            if(copyFromOriginal) {
                CopyFromOriginal(rect);
            }
        }

        /// <summary>Get pixels as <see cref="Span{T}"/> of <see cref="ColorByte"/>.</summary>
        /// <remarks>If change pixels via span, you must call <see cref="SetDirty"/> method after that.</remarks>
        /// <returns><see cref="Span{T}"/> of <see cref="ColorByte"/></returns>
        public Span<ColorByte> AsSpan() => MemoryMarshal.CreateSpan(ref *Ptr, _rect.Width * _rect.Height);

        /// <summary>Flush changes of pixels to <see cref="Texture"/> if dirty flag is set.</summary>
        /// <remarks>This method is automatically called from <see cref="Dispose"/></remarks>
        public void Flush()
        {
            if(_isDirty) {
                Debug.Assert(_pixels != null);
                _t.UpdateSubTexture(_pixels, _rect);
                _isDirty = false;
            }
        }

        public void Dispose()
        {
            Flush();
            _paint?.Dispose();
            _bitmap?.Dispose();
            _canvas?.Dispose();
            _textBuilder?.Dispose();
        }

        private void CopyFromOriginal(in RectI rect)
        {
            var texture = _t;
            var pixCount = texture.Size.X * texture.Size.Y;
            var buf = new Span<ColorByte>(Bitmap.GetPixels().ToPointer(), pixCount);
            texture.GetPixels(rect, buf);
        }

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
                _pixels = (ColorByte*)Bitmap.GetPixels();
            }
        }

        private static SKColor GetSKColor(in ColorByte color)
        {
            // Don't use 'Unsafe.As'.
            return new SKColor(color.R, color.G, color.B, color.A);
        }
    }
}
