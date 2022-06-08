#nullable enable
using SkiaSharp;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Elffy
{
    public sealed class Font : IDisposable
    {
        private SKFont? _skFont;
        private float _size;

        public float Size => _size;

        public Font(float size) : this(Typeface.Default, size)
        {
        }

        public Font(Stream stream, float size) : this(new Typeface(stream), size)
        {
        }

        public Font(Typeface typeface, float size)
        {
            if(typeface is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(typeface));
            }
            if(size <= 0) {
                ThrowOutOfRange();
                [DoesNotReturn] static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(size));
            }

            _size = size;
            _skFont = new SKFont(typeface.GetSKTypeface(), size);
            _skFont.Subpixel = true;
        }

        public Font(SKFont skFont)
        {
            if(skFont is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(skFont));
            }

            _size = skFont.Size;
            _skFont = skFont;
        }

        internal SKFont GetSKFont()
        {
            if(_skFont is null) {
                ThrowDisposed();
                [DoesNotReturn] static void ThrowDisposed() => throw new ObjectDisposedException(typeof(Font).FullName);
            }
            return _skFont;
        }

        public void Dispose()
        {
            _skFont?.Dispose();
            _skFont = null;
        }
    }
}
