#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Elffy.Effective;
using Elffy.Effective.Unsafes;
using Elffy.Imaging.Internal;

namespace Elffy.Imaging
{
    public unsafe readonly struct Icon : IDisposable, IEquatable<Icon>
    {
        private readonly Image[]? _images;

        //public ReadOnlySpan<Image> Images => _images.AsSpan();

        public int ImageCount => _images?.Length ?? 0;

        public static Icon None => default;

        [Obsolete("Don't use defaut constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public Icon() => throw new NotSupportedException("Don't use defaut constructor.");

        private Icon(int count)
        {
            _images = new Image[count];
        }

        public static Icon Create<TState>(int imageCount, TState state, IconCreateAction<TState> action)
        {
            if(action is null) {
                ThrowNullArg();
                [DoesNotReturn] static void ThrowNullArg() => throw new ArgumentNullException(nameof(action));
            }
            var icon = new Icon(imageCount);
            try {
                action.Invoke(icon._images.AsSpan(), state);
                return icon;
            }
            catch {
                icon.Dispose();
                throw;
            }
        }

        public delegate void IconCreateAction<in TArg>(Span<Image> images, TArg arg);

        public ImageRef GetImage(int index)
        {
            if(_images is null || (uint)index >= _images.Length) {
                ThrowHelper.ThrowArgOutOfRange(nameof(index));
            }
            return _images.At(index);
        }

        public void Dispose()
        {
            var images = Interlocked.Exchange(ref Unsafe.AsRef(in _images), null);
            if(images is null) { return; }
            for(int i = 0; i < images.Length; i++) {
                images[i].Dispose();
            }
        }

        public Icon Clone()
        {
            var images = _images;
            if(images is null || images.Length == 0) {
                return None;
            }
            return Create(images.Length, images, static (newImages, originals) => originals.CopyTo(newImages));
        }

        public override bool Equals(object? obj) => obj is Icon icon && Equals(icon);

        public bool Equals(Icon other) => _images == other._images;

        public override int GetHashCode() => _images?.GetHashCode() ?? 0;

        public static bool operator ==(Icon left, Icon right) => left.Equals(right);

        public static bool operator !=(Icon left, Icon right) => !(left == right);
    }
}
