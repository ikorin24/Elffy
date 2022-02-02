#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Effective;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy.Threading
{
    public static class UniTaskMemoryPool
    {
        public static UniTaskRentArray Rent(int minLength)
        {
            if(minLength < 0) {
                ThrowArgOutOfRange();
                return default;
                [DoesNotReturn] static void ThrowArgOutOfRange() => throw new ArgumentOutOfRangeException(nameof(minLength));
            }
            else if(minLength == 0) {
                return UniTaskRentArray.Empty;
            }
            else if(minLength <= 16) {
                if(ChainInstancePool<UniTaskArray16>.TryGetInstanceFast(out var instance) == false) {
                    instance = new UniTaskArray16();
                }
                return new UniTaskRentArray(instance);
            }
            else if(minLength <= 64) {
                if(ChainInstancePool<UniTaskArray64>.TryGetInstanceFast(out var instance) == false) {
                    instance = new UniTaskArray64();
                }
                return new UniTaskRentArray(instance);
            }
            else {
                return new UniTaskRentArray(new UniTask[minLength]);
            }
        }

        public static void Return(UniTaskRentArray rentArray)
        {
            if(rentArray.TryExtractArray16(out var array16)) {
                // The array must be cleared because UniTask contains a reference type field.
                array16.AsSpan().Clear();
                ChainInstancePool<UniTaskArray16>.ReturnInstance(array16);
            }
            else if(rentArray.TryExtractArray64(out var array64)) {
                // The array must be cleared because UniTask contains a reference type field.
                array64.AsSpan().Clear();
                ChainInstancePool<UniTaskArray64>.ReturnInstance(array64);
            }
        }

        internal sealed class UniTaskArray16 : IChainInstancePooled<UniTaskArray16>
        {
            private UniTaskArray16Core _core;
            private UniTaskArray16? _next;

            public int Length => UniTaskArray16Core.ElementCount;

            public ref UniTask this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    if((uint)index >= Length) { throw new ArgumentOutOfRangeException(nameof(index)); }
                    return ref Unsafe.Add(ref _core.E0, index);
                }
            }

            public ref UniTaskArray16? NextPooled => ref _next;

            static UniTaskArray16()
            {
                ChainInstancePool<UniTaskArray16>.SetMaxPoolingCount(64);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref UniTask GetReference() => ref _core.E0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<UniTask> AsSpan()
            {
                return MemoryMarshal.CreateSpan(ref _core.E0, UniTaskArray16Core.ElementCount);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<UniTask> AsSpan(int start, int length)
            {
                if((uint)start >= UniTaskArray16Core.ElementCount) {
                    ThrowArgOutOfRange();
                    [DoesNotReturn] static void ThrowArgOutOfRange() => throw new ArgumentOutOfRangeException(nameof(start));
                }
                if((uint)length > (uint)(UniTaskArray16Core.ElementCount - start)) {
                    ThrowArgOutOfRange();
                    [DoesNotReturn] static void ThrowArgOutOfRange() => throw new ArgumentOutOfRangeException(nameof(length));
                }
                return MemoryMarshal.CreateSpan(ref Unsafe.Add(ref _core.E0, start), length);
            }
        }

        internal sealed class UniTaskArray64 : IChainInstancePooled<UniTaskArray64>
        {
            private UniTaskArray64Core _core;
            private UniTaskArray64? _next;

            public int Length => UniTaskArray64Core.ElementCount;

            public ref UniTask this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    if((uint)index >= Length) { throw new ArgumentOutOfRangeException(nameof(index)); }
                    return ref Unsafe.Add(ref _core.Array0.E0, index);
                }
            }

            public ref UniTaskArray64? NextPooled => ref _next;

            static UniTaskArray64()
            {
                ChainInstancePool<UniTaskArray64>.SetMaxPoolingCount(32);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ref UniTask GetReference() => ref _core.Array0.E0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<UniTask> AsSpan()
            {
                return MemoryMarshal.CreateSpan(ref _core.Array0.E0, UniTaskArray64Core.ElementCount);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Span<UniTask> AsSpan(int start, int length)
            {
                if((uint)start >= UniTaskArray64Core.ElementCount) {
                    ThrowArgOutOfRange();
                    [DoesNotReturn] static void ThrowArgOutOfRange() => throw new ArgumentOutOfRangeException(nameof(start));
                }
                if((uint)length > (uint)(UniTaskArray64Core.ElementCount - start)) {
                    ThrowArgOutOfRange();
                    [DoesNotReturn] static void ThrowArgOutOfRange() => throw new ArgumentOutOfRangeException(nameof(length));
                }
                return MemoryMarshal.CreateSpan(ref Unsafe.Add(ref _core.Array0.E0, start), length);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        private struct UniTaskArray16Core
        {
            public const int ElementCount = 16;

            public UniTask E0;
            public UniTask E1;
            public UniTask E2;
            public UniTask E3;
            public UniTask E4;
            public UniTask E5;
            public UniTask E6;
            public UniTask E7;
            public UniTask E8;
            public UniTask E9;
            public UniTask E10;
            public UniTask E11;
            public UniTask E12;
            public UniTask E13;
            public UniTask E14;
            public UniTask E15;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        private struct UniTaskArray64Core
        {
            public const int ElementCount = 64;

            public UniTaskArray16Core Array0;
            public UniTaskArray16Core Array1;
            public UniTaskArray16Core Array2;
            public UniTaskArray16Core Array3;
        }
    }
}
