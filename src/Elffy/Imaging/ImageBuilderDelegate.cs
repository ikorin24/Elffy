#nullable enable
using System;
using System.Runtime.CompilerServices;
using Elffy.Effective.Unsafes;

namespace Elffy.Imaging
{
    public unsafe readonly struct ImageBuilderDelegate<T> : IEquatable<ImageBuilderDelegate<T>>
    {
        private readonly delegate*<T, ImageRef, void> _func;

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

        // [NOTE]
        // No one can make an instance of type 'NullLiteral' in the usual way.
        // The only way you can use the following operator method is to specify null literal.
        // So they work well.
        // 
        // I make sure 'NullLiteral' instance is actual null just in case.
        // In the case that users set null literal and the methods get inlined,
        // the check is removed. Therefore, it is no-cost.

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(ImageBuilderDelegate<T> d, NullLiteral @null) => @null is null && d._func == null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(ImageBuilderDelegate<T> d, NullLiteral @null) => !(d == @null);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(NullLiteral @null, ImageBuilderDelegate<T> d) => d == @null;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(NullLiteral @null, ImageBuilderDelegate<T> d) => !(d == @null);
    }
}
