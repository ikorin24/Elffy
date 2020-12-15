#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using Elffy.Effective;
using Elffy.Imaging;
using SkiaSharp;

namespace Elffy.Components
{
    /// <summary>Painter object for <see cref="Texture"/></summary>
    public unsafe struct TexturePainter : IDisposable
    {
        // This color type is same as texture inner pixel format of opengl, and Elffy.ColorByte
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
        private SKBitmap Bitmap => _bitmap ??= new SKBitmap(new SKImageInfo(_rect.Width, _rect.Height, ColorType, SKAlphaType.Premul));
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
                texture.GetPixels(rect, new(Bitmap.GetPixels().ToPointer(), rect.Width * rect.Height));
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
                void FlushCore(ref TexturePainter @this)
                {
                    Debug.Assert(@this._pixels != null);
                    @this._t.UpdateSubTexture(@this._pixels, @this._rect);
                    @this._isDirty = false;
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
        public void Fill(in ColorByte color)
        {
            var canvas = Canvas;
            canvas.Clear(GetSKColor(color));
            SetDirty();
        }

        public void DrawBitmap(Bitmap bitmap)
        {
            DrawBitmap(bitmap, new Vector2i(0, 0));
        }

        public void DrawBitmap(Bitmap bitmap, in Vector2i offset)
        {
            if((uint)offset.X >= _rect.Width) {
                ThrowOutOfRange();
            }
            if((uint)offset.Y >= _rect.Height) {
                ThrowOutOfRange();
            }
            [DoesNotReturn] static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(offset));


            using var bitmapPixels = bitmap.GetPixels(ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            var ptr = Ptr;
            var height = Math.Min(bitmapPixels.Height, _rect.Height - offset.Y);
            for(int row = 0; row < height; row++) {
                var destRowLine = MemoryMarshal.CreateSpan(ref Unsafe.AsRef<byte>(ptr + (row + offset.Y) * _rect.Width + offset.X),
                                                           (_rect.Width - offset.X) * sizeof(ColorByte));
                var srcRowLine = bitmapPixels.GetRowLine(row);
                srcRowLine.Slice(0, Math.Min(srcRowLine.Length, destRowLine.Length))
                          .CopyTo(destRowLine);

                var destPix = destRowLine.MarshalCast<byte, ColorByte>();
                for(int i = 0; i < destPix.Length; i++) {

                    // Swap R and B because System.Drawing.Bitmap is (B, G, R, A) but I need (R, G, B, A).
                    (destPix[i].R, destPix[i].B) = (destPix[i].B, destPix[i].R);
                }
            }
            SetDirty();
        }

        public void DrawText(string text, SKFont font, in Vector2 pos, in ColorByte color)
        {
            DrawText(text.AsSpan(), font, pos, color);
        }

        public void DrawText(ReadOnlySpan<char> text, SKFont font, in Vector2 pos, in ColorByte color)
        {
            if(font is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(font));
            }

            DrawTextCore(text.MarshalCast<char, byte>(), SKTextEncoding.Utf16, font, pos, GetSKColor(color));
        }

        public void DrawText(ReadOnlySpan<byte> utf8Text, SKFont font, in Vector2 pos, in ColorByte color)
        {
            if(font is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(font));
            }

            DrawTextCore(utf8Text, SKTextEncoding.Utf8, font, pos, GetSKColor(color));
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

        private void SetDirty()
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
