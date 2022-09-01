#nullable enable
using Elffy.Effective.Unsafes;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy.Effective
{
    /// <summary>Shared memories from memory pool, that provides <see cref="Span{T}"/>.</summary>
    /// <remarks>
    /// Don't call <see cref="Dispose"/> twice.
    /// </remarks>
    /// <typeparam name="T">element type</typeparam>
    [DebuggerDisplay("{DebugDisplay}")]
    [DebuggerTypeProxy(typeof(ValueTypeRentMemoryDebuggerTypeProxy<>))]
    public readonly struct ValueTypeRentMemory<T> : IEquatable<ValueTypeRentMemory<T>>, IDisposable, ISpan<T> where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly string DebugDisplay => $"{nameof(ValueTypeRentMemory<T>)}<{typeof(T).Name}>[{_length}]";

        // [In the case of rent array]
        // _array : rent array
        // _start : indicates the start position of available memory; _array[(int)_start]
        // _length : number of T element
        //
        // [When allocate unmanaged memory]
        // _array : null
        // _start : unmanaged pointer
        // _length : number of T element

        private readonly byte[]? _array;
        private readonly IntPtr _start;
        private readonly int _length;

        public static ValueTypeRentMemory<T> Empty => default;

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
        public ValueTypeRentMemory() => throw new NotSupportedException("Don't use default constructor.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ValueTypeRentMemory(int length, bool zeroFill)
        {
            if(length == 0) {
                this = default;
                return;
            }
            if(length < 0) {
                ThrowArgOutOfRange();
                [DoesNotReturn] static void ThrowArgOutOfRange() => throw new ArgumentOutOfRangeException(nameof(length));
            }
            if(ArrayMemoryPool.TryRentValueTypeMemory<T>(length, out _array, out int start)) {
                Debug.Assert(_array is not null);
                _start = new IntPtr(start);
            }
            else {
                if(length > int.MaxValue / sizeof(T)) {
                    ThrowTooLarge();
                }
                Debug.Assert(_array is null);
                Debug.Assert(start == 0);
                _start = Marshal.AllocHGlobal(sizeof(T) * length);
            }
            _length = length;

            if(zeroFill) {
                AsSpan().Clear();
            }

            [DoesNotReturn] static void ThrowTooLarge() => throw new ArgumentOutOfRangeException(nameof(length) + " is too large.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ValueTypeRentMemory(int length, bool zeroFill, out Span<T> span) : this(length, zeroFill)
        {
            span = AsSpan();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref T GetReference()
        {
            if(_array is null) {
                return ref Unsafe.AsRef<T>(_start.ToPointer());
            }
            else {
                return ref Unsafe.As<byte, T>(ref _array.At(_start.ToInt32()));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref GetReference(), _length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan(int start) => AsSpan().Slice(start);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan(int start, int length) => AsSpan().Slice(start, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> AsReadOnlySpan() => MemoryMarshal.CreateSpan(ref GetReference(), _length);

        public T[] ToArray() => AsSpan().ToArray();

        /// <summary>Release the memory</summary>
        /// <remarks>*** Don't call this method twice. ***</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if(_array is null) {
                Marshal.FreeHGlobal(_start);
            }
            else if(_length != 0) {
                ArrayMemoryPool.ReturnValueTypeMemory(_array, (int)_start);
                Unsafe.AsRef<byte[]?>(_array) = null;
            }
            Unsafe.AsRef(_start) = IntPtr.Zero;
            Unsafe.AsRef(_length) = 0;
        }

        public override bool Equals(object? obj) => obj is ValueTypeRentMemory<T> memory && Equals(memory);

        public override int GetHashCode() => HashCode.Combine(_array, _start, _length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ValueTypeRentMemory<T> other)
        {
            return ReferenceEquals(_array, other._array) &&
                   _start == other._start &&
                   _length == other._length;
        }

        public override string ToString() => DebugDisplay;
    }

    internal sealed class ValueTypeRentMemoryDebuggerTypeProxy<T> where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ValueTypeRentMemory<T> _entity;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => _entity.ToArray();

        internal ValueTypeRentMemoryDebuggerTypeProxy(ValueTypeRentMemory<T> entity)
        {
            _entity = entity;
        }
    }
}
