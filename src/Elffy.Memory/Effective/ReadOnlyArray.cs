#nullable enable
using Elffy.Effective.Unsafes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Elffy.Effective
{
    [Obsolete("Don't use the class. Not implemented yet.", true)]
    [DebuggerDisplay("ReadOnlyArray<{typeof(T).Name,nq}>[{Length}]")]
    [DebuggerTypeProxy(typeof(ReadOnlyArrayTypeProxy<>))]
    public readonly struct ReadOnlyArray<T> : IEquatable<ReadOnlyArray<T>>
    {
        private readonly T[]? _array;

        public static ReadOnlyArray<T> Empty => default;

        public int Length => _array?.Length ?? 0;

        public readonly ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var array = _array;
                if(array is not null) {
                    if((uint)index < array.Length) {
                        return ref array.At(index);
                    }
                }
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public ReadOnlyArray(T[]? array)
        {
            _array = array;
        }

        public ReadOnlySpan<T> AsSpan() => _array.AsSpan();

        public T[] ToArray() => _array.AsSpan().ToArray();

        public IEnumerable<T> AsEnumerable() => _array ?? Array.Empty<T>();

        public override bool Equals(object? obj) => obj is ReadOnlyArray<T> array && Equals(array);

        public bool Equals(ReadOnlyArray<T> other) => _array == other._array;

        public override int GetHashCode() => _array?.GetHashCode() ?? 0;

        public static bool operator ==(ReadOnlyArray<T> left, ReadOnlyArray<T> right) => left.Equals(right);

        public static bool operator !=(ReadOnlyArray<T> left, ReadOnlyArray<T> right) => !(left == right);
    }

    [Obsolete("Don't use the class. Not implemented yet.", true)]
    internal sealed class ReadOnlyArrayTypeProxy<T>
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ReadOnlyArray<T> _array;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => _array.ToArray();

        public ReadOnlyArrayTypeProxy(ReadOnlyArray<T> array) => _array = array;
    }

    public static class ReadOnlyArrayExtensions
    {
        [Obsolete("Don't use the class. Not implemented yet.", true)]
        public static ReadOnlyArray<T> AsReadOnlyArray<T>(this T[]? array)
        {
            return new ReadOnlyArray<T>(array);
        }
    }
}
