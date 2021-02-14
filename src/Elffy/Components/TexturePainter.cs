#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using Elffy.Effective;
using Elffy.Imaging;
using Elffy.OpenGL;
using SkiaSharp;

namespace Elffy.Components
{
    /// <summary>Painter object for <see cref="Texture"/></summary>
    public unsafe struct TexturePainter : IDisposable
    {
        // This color type is same as texture inner pixel format of opengl, and Elffy.ColorByte
        const SKColorType ColorType = SKColorType.Rgba8888;

        private readonly RectI _rect;
        private readonly TextureObject _t;
        private SKPaint? _paint;
        private SKBitmap? _bitmap;
        private SKCanvas? _canvas;
        private SKTextBlobBuilder? _textBuilder;
        private bool _isDirty;
        private ColorByte* _pixels;      // pointer to a head pixel

        private SKPaint Paint => _paint ??= new SKPaint();
        private SKBitmap Bitmap => _bitmap ??= new SKBitmap(new SKImageInfo(_rect.Width, _rect.Height, ColorType, SKAlphaType.Premul));
        private SKCanvas Canvas => _canvas ??= new SKCanvas(Bitmap);
        private SKTextBlobBuilder TextBuilder => _textBuilder ??= new SKTextBlobBuilder();

        /// <summary>Get pointer to pixels.</summary>
        /// <remarks>If change pixels via pointer, you must call <see cref="SetDirty"/> method after that.</remarks>
        public ColorByte* Ptr => (ColorByte*)Bitmap.GetPixels();

        internal TexturePainter(in TextureObject texture, in RectI rect, bool copyFromOriginal)
        {
            // I don't check that rect is valid here.
            _rect = rect;
            _t = texture;
            _isDirty = false;
            _pixels = default;
            _paint = null;
            _bitmap = null;
            _canvas = null;
            _textBuilder = null;
            if(copyFromOriginal) {
                TextureCore.GetPixels(texture, rect, Pixels());
            }
        }

        /// <summary>Get pixels as <see cref="Span{T}"/> of <see cref="ColorByte"/>.</summary>
        /// <remarks>If change pixels via span, you must call <see cref="SetDirty"/> method after that.</remarks>
        /// <returns><see cref="Span{T}"/> of <see cref="ColorByte"/></returns>
        public Span<ColorByte> Pixels() => MemoryMarshal.CreateSpan(ref Unsafe.AsRef<ColorByte>(Ptr), _rect.Width * _rect.Height);

        /// <summary>Get pixels of a specified row line</summary>
        /// <param name="row">number of the row</param>
        /// <returns>pixels</returns>
        public Span<ColorByte> GetRowLine(int row)
        {
            if((uint)row >= (uint)_rect.Height) {
                ThrowOutOfRange();
                [DoesNotReturn] static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException();
            }
            return MemoryMarshal.CreateSpan(ref Unsafe.AsRef<ColorByte>(Ptr + row * _rect.Width), _rect.Width);
        }

        /// <summary>Flush changes of pixels to <see cref="Texture"/> if dirty flag is set.</summary>
        /// <remarks>This method is automatically called from <see cref="Dispose"/></remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Flush()
        {
            if(_isDirty) {
                FlushCore(ref this);

                [MethodImpl(MethodImplOptions.NoInlining)]  // no inlining
                static void FlushCore(ref TexturePainter painter)
                {
                    Debug.Assert(painter._pixels != null);
                    TextureCore.UpdateSubTexture(painter._t, painter._pixels, painter._rect);
                    painter._isDirty = false;
                }
            }
        }

        /// <summary>Flush modification and dispose the painter</summary>
        public void Dispose()
        {
            Flush();
            _paint?.Dispose();
            _bitmap?.Dispose();
            _canvas?.Dispose();
            _textBuilder?.Dispose();
        }

        /// <summary>Fill the rect by specified color</summary>
        /// <param name="color">color to fill the rect</param>
        public void Fill(ColorByte color)
        {
            var canvas = Canvas;
            canvas.Clear(GetSKColor(color));
            SetDirty();
        }

        public void DrawLine(in Vector2 p0, in Vector2 p1, ColorByte color, float width)
        {
            DrawLine(p0.X, p0.Y, p1.X, p1.Y, color, width);
        }

        public void DrawLine(float x0, float y0, float x1, float y1, ColorByte color, float width)
        {
            var canvas = Canvas;
            var paint = Paint;
            paint.Reset();
            paint.Color = GetSKColor(color);
            paint.IsAntialias = true;
            paint.StrokeWidth = width;
            canvas.DrawLine(x0, y0, x1, y1, paint);
            SetDirty();
        }

        public void DrawImage(in Image image)
        {
            DrawImage(image, Vector2i.Zero);
        }

        public void DrawImage(in Image image, in Vector2i offset)
        {
            if((uint)offset.X >= _rect.Width) {
                ThrowOutOfRange();
            }
            if((uint)offset.Y >= _rect.Height) {
                ThrowOutOfRange();
            }
            [DoesNotReturn] static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(offset));

            var ptr = Ptr;
            var height = Math.Min(image.Height, _rect.Height - offset.Y);
            for(int row = 0; row < height; row++) {
                var destRowLine = MemoryMarshal.CreateSpan(ref *(ptr + (row + offset.Y) * _rect.Width + offset.X), _rect.Width - offset.X);
                var srcRowLine = image.GetRowLine(row);
                srcRowLine.Slice(0, Math.Min(srcRowLine.Length, destRowLine.Length)).CopyTo(destRowLine);
            }
            SetDirty();
        }

        public void DrawText(string text, Font font, in Vector2 pos, ColorByte color)
        {
            DrawText(text.AsSpan(), font, pos, color);
        }

        public void DrawText(ReadOnlySpan<char> text, Font font, in Vector2 pos, ColorByte color)
        {
            if(font is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(font));
            }

            DrawTextCore(text.MarshalCast<char, byte>(), SKTextEncoding.Utf16, font, pos, GetSKColor(color));
        }

        public void DrawText(ReadOnlySpan<byte> utf8Text, Font font, in Vector2 pos, ColorByte color)
        {
            if(font is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(font));
            }

            DrawTextCore(utf8Text, SKTextEncoding.Utf8, font, pos, GetSKColor(color));
        }

        private void DrawTextCore(ReadOnlySpan<byte> text, SKTextEncoding enc, Font font, in Vector2 pos, SKColor color)
        {
            var skFont = font.GetSKFont();
            var glyphCount = skFont.CountGlyphs(text, enc);
            if(glyphCount == 0) {
                return;
            }

            var builder = TextBuilder;

            var buffer = builder.AllocatePositionedRun(skFont, glyphCount);
            var glyphs = buffer.GetGlyphSpan();
            skFont.GetGlyphs(text, enc, glyphs);
            skFont.GetGlyphPositions(glyphs, buffer.GetPositionSpan());

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static SKColor GetSKColor(ColorByte color)
        {
            // Don't use 'Unsafe.As'.
            return new SKColor(color.R, color.G, color.B, color.A);
        }
    }
}
