#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Elffy.Exceptions;

namespace Elffy.Effective
{
    /// <summary>Disposable <see cref="Span{T}"/> of unmanaged heap memory.</summary>
    /// <typeparam name="T"></typeparam>
    public unsafe readonly struct UnmanagedBuffer<T> where T : unmanaged
    {
        private readonly T* _pointer;
        private readonly int _length;

        /// <summary>
        /// Allocate unmanaged heap memory of specified length.<para/>
        /// [NOTE] You MUST call <see cref="Dispose"/> to release memory !!! Or memory leak occurs.
        /// </summary>
        /// <param name="length">element length (not bytes length)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnmanagedBuffer(int length)
        {
            ArgumentChecker.ThrowOutOfRangeIf(length <= 0);
            _pointer = (T*)Marshal.AllocHGlobal(sizeof(T) * length);
            _length = length;
        }

        /// <summary>Get <see cref="Span{T}"/></summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly Span<T> GetSpan() => new Span<T>(_pointer, _length);

        /// <summary>Release memory.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => Release(in this);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Release(in UnmanagedBuffer<T> buffer)
        {
            ref var mock = ref Unsafe.As<UnmanagedBuffer<T>, Mock>(ref Unsafe.AsRef(in buffer));
            if(mock.Length > 0) {
                Marshal.FreeHGlobal(new IntPtr(mock.Pointer));
                mock.Pointer = (void*)IntPtr.Zero;
                mock.Length = 0;
            }
        }

        private unsafe struct Mock
        {
            public void* Pointer;
            public int Length;
        }
    }
}
