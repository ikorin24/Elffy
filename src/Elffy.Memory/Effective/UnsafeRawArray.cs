#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Elffy.Effective
{
    /// <summary>Low level wrapper of malloc, free. There are no safety checking, no zero initialized</summary>
    public readonly unsafe struct UnsafeRawArray<T> : IDisposable where T : unmanaged
    {
        public readonly int Length;
        public readonly IntPtr Ptr;

        public static UnsafeRawArray<T> Empty => default;

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref Unsafe.Add(ref Reference(), index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeRawArray(int length)
        {
            if(length < 0) {
                throw new ArgumentOutOfRangeException();
            }
            if(length == 0) {
                this = default;
                return;
            }
            Length = length;
            Ptr = Marshal.AllocHGlobal(length * sizeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeRawArray(int length, bool zeroFill)
        {
            if(length < 0) {
                throw new ArgumentOutOfRangeException();
            }
            if(length == 0) {
                this = default;
                return;
            }
            Length = length;
            Ptr = Marshal.AllocHGlobal(length * sizeof(T));
            if(zeroFill) {
                MemoryMarshal.CreateSpan(ref Unsafe.AsRef<T>((T*)Ptr), length).Clear();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            Marshal.FreeHGlobal(Ptr);
            Unsafe.AsRef(Length) = 0;
            Unsafe.AsRef(Ptr) = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T Reference() => ref Unsafe.AsRef<T>((void*)Ptr);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan()
        {
            return MemoryMarshal.CreateSpan(ref Reference(), Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan(int start)
        {
            return MemoryMarshal.CreateSpan(ref this[start], Length - start);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan(int start, int length)
        {
            return MemoryMarshal.CreateSpan(ref this[start], length);
        }
    }
}
