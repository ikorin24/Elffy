#nullable enable
using System;
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

        public ReadOnlySpan<Image> Images => _images.AsSpan();

        public int ImageCount => _images?.Length ?? 0;

        public static Icon None => default;

        public Icon(ReadOnlySpan<Image> images) : this(images, MemoryCopyMode.DeepCopy)
        {
        }

        public Icon(ReadOnlySpan<Image> images, MemoryCopyMode copyMode)
        {
            if(images.Length == 0) {
                _images = null;
                return;
            }

            var iconImages = new Image[images.Length];  // TODO: instance pooling ?
            if(copyMode == MemoryCopyMode.ArrayOnly) {
                images.CopyTo(iconImages.AsSpanUnsafe());
            }
            else if(copyMode == MemoryCopyMode.DeepCopy) {
                for(int i = 0; i < images.Length; i++) {
                    iconImages[i] = images[i].ToImage();
                }
            }
            else {
                ThrowHelper.ThrowArgException(nameof(copyMode));
            }
            _images = iconImages;
        }

        internal Icon(int count)
        {
            _images = new Image[count];  // TODO: instance pooling ?
        }

        public ref readonly Image GetImage(int index)
        {
            if(_images is null || (uint)index >= _images.Length) {
                ThrowHelper.ThrowArgOutOfRange(nameof(index));
            }
            return ref _images.At(index);
        }

        internal Span<Image> GetImagesSpan() => _images.AsSpan();

        public void Dispose()
        {
            var images = Interlocked.Exchange(ref Unsafe.AsRef(in _images), null);
            if(images is null) { return; }
            for(int i = 0; i < images.Length; i++) {
                images[i].Dispose();
            }
        }

        public Icon Clone() => Clone(MemoryCopyMode.DeepCopy);

        public Icon Clone(MemoryCopyMode copyMode)
        {
            return new Icon(_images, copyMode);
        }

        public override bool Equals(object? obj) => obj is Icon icon && Equals(icon);

        public bool Equals(Icon other) => _images == other._images;

        public override int GetHashCode() => _images?.GetHashCode() ?? 0;

        public static bool operator ==(Icon left, Icon right) => left.Equals(right);

        public static bool operator !=(Icon left, Icon right) => !(left == right);
    }
}
