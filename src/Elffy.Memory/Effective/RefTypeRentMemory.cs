#nullable enable
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Elffy.Effective.Unsafes;

namespace Elffy.Effective
{
    /// <summary>Shared memories from memory pool, that provides <see cref="Span{T}"/>.</summary>
    /// <remarks>
    /// Don't call <see cref="Dispose"/> twice.
    /// </remarks>
    /// <typeparam name="T">element type</typeparam>
    [DebuggerDisplay("{DebugDisplay}")]
    [DebuggerTypeProxy(typeof(RefTypeRentMemoryDebuggerTypeProxy<>))]
    public readonly struct RefTypeRentMemory<T> : IEquatable<RefTypeRentMemory<T>>, IDisposable where T : class?
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebugDisplay => $"{nameof(RefTypeRentMemory<T>)}<{typeof(T).Name}>[{_length}]";

        private readonly object?[]? _array;
        private readonly int _start;
        private readonly int _length;

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _length;
        }

        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _length == 0;
        }

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if((uint)index >= (uint)_length) {
                    ThrowOutOfRange();
                    static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(index));
                }
                return ref Unsafe.Add(ref GetReference(), index);
            }
        }

        [Obsolete("Don't use default constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public RefTypeRentMemory() => throw new NotSupportedException("Don't use default constructor.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RefTypeRentMemory(int length)
        {
            if(length == 0) {
                this = default;
                return;
            }
            if(length < 0) {
                ThrowArgOutOfRange();
                [DoesNotReturn] static void ThrowArgOutOfRange() => throw new ArgumentOutOfRangeException(nameof(length));
            }
            if(ArrayMemoryPool.TryRentRefTypeMemory(length, out _array, out _start) == false) {
                Debug.Assert(_array is null);
                _start = 0;
                _array = new object[length];
            }
            _length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref T GetReference()
        {
            if(_array is null) {
                return ref Unsafe.AsRef<T>(null);
            }
            else {
                return ref Unsafe.As<object?, T>(ref _array.At(_start));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref GetReference(), _length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan(int start) => AsSpan().Slice(start);

        public Span<T> AsSpan(int start, int length) => AsSpan().Slice(start, length);

        public T[] ToArray() => AsSpan().ToArray();

        /// <summary>Release the memory</summary>
        /// <remarks>*** Don't call this method twice. ***</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if(_length != 0) {
                Debug.Assert(_array is not null);
                AsSpan().Clear();           // All elements MUST be cleared, or elements are not collected by GC.
                ArrayMemoryPool.ReturnRefTypeMemory(_array, _start);
                Unsafe.AsRef<object?[]?>(_array) = null;
                Unsafe.AsRef(_start) = 0;
                Unsafe.AsRef(_length) = 0;
            }
        }

        public override bool Equals(object? obj) => obj is RefTypeRentMemory<T> memory && Equals(memory);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(RefTypeRentMemory<T> other)
        {
            return _array == other._array &&
                   _start == other._start &&
                   _length == other._length;
        }

        public override int GetHashCode() => HashCode.Combine(_array, _start, _length);

        public override string ToString() => DebugDisplay;
    }

    internal sealed class RefTypeRentMemoryDebuggerTypeProxy<T> where T : class?
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly RefTypeRentMemory<T> _entity;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => _entity.ToArray();

        internal RefTypeRentMemoryDebuggerTypeProxy(RefTypeRentMemory<T> entity)
        {
            _entity = entity;
        }
    }
}
