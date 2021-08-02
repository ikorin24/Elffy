#nullable enable
using System;

namespace Elffy.Imaging
{
    /// <summary>Wrapper of <see langword="delegate"/>*&lt;<typeparamref name="T"/>, <see cref="ImageRef"/>, <see langword="void"/>&gt;</summary>
    /// <typeparam name="T">type of state</typeparam>
    public unsafe readonly struct ImageBuilderDelegate<T> : IEquatable<ImageBuilderDelegate<T>>
    {
        private readonly delegate*<T, ImageRef, void> _func;

        public bool IsNull => _func == null;

        public ImageBuilderDelegate(delegate*<T, ImageRef, void> func) => _func = func;

        public void Invoke(T state, ImageRef image) => _func(state, image);

        public void InvokeIfNotNull(T state, ImageRef image)
        {
            if(_func != null) {
                _func(state, image);
            }
        }

        public override bool Equals(object? obj) => obj is ImageBuilderDelegate<T> @delegate && Equals(@delegate);
        public bool Equals(ImageBuilderDelegate<T> other) => _func == other._func;
        public override int GetHashCode() => new IntPtr(_func).GetHashCode();
        public static bool operator ==(ImageBuilderDelegate<T> left, ImageBuilderDelegate<T> right) => left.Equals(right);
        public static bool operator !=(ImageBuilderDelegate<T> left, ImageBuilderDelegate<T> right) => !(left == right);
    }
}
