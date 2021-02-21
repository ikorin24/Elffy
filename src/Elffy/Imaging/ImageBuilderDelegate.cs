#nullable enable
using System;

namespace Elffy.Imaging
{
    internal unsafe readonly struct ImageBuilderDelegate : IEquatable<ImageBuilderDelegate>
    {
        private readonly delegate*<Vector2i, ColorByte*, void> _func;

        public ImageBuilderDelegate(delegate*<Vector2i, ColorByte*, void> func) => _func = func;

        public void Invoke(Vector2i size, ColorByte* pixels) => _func(size, pixels);

        public override bool Equals(object? obj) => obj is ImageBuilderDelegate @delegate && Equals(@delegate);
        public bool Equals(ImageBuilderDelegate other) => _func == other._func;
        public override int GetHashCode() => new IntPtr(_func).GetHashCode();
        public static bool operator ==(ImageBuilderDelegate left, ImageBuilderDelegate right) => left.Equals(right);
        public static bool operator !=(ImageBuilderDelegate left, ImageBuilderDelegate right) => !(left == right);
    }
}
