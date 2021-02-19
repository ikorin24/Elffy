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

        public static Icon Empty => default;

        public Icon(Span<Image> images)
        {
            _images = new Image[images.Length];  // TODO: instance pooling ?
            images.CopyTo(_images.AsSpanUnsafe());
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

        public override bool Equals(object? obj) => obj is Icon icon && Equals(icon);

        public bool Equals(Icon other) => _images == other._images;

        public override int GetHashCode() => _images?.GetHashCode() ?? 0;

        public static bool operator ==(Icon left, Icon right) => left.Equals(right);

        public static bool operator !=(Icon left, Icon right) => !(left == right);
    }
}
