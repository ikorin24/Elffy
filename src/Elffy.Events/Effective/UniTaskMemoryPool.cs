#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy.Effective
{
    public static class UniTaskMemoryPool
    {
        public static UniTaskRentArray Rent(int length)
        {
            if(length < 0) {
                ThrowArgOutOfRange();
                return default;
                [DoesNotReturn] static void ThrowArgOutOfRange() => throw new ArgumentOutOfRangeException(nameof(length));
            }
            else if(length == 0) {
                return UniTaskRentArray.Empty;
            }
            else if(length <= 16) {
                if(ChainInstancePool<UniTaskArray16>.TryGetInstanceFast(out var instance) == false) {
                    instance = new UniTaskArray16();
                }
                return new UniTaskRentArray(instance, length);
            }
            else {
                return new UniTaskRentArray(new UniTask[length]);
            }
        }

        public static void Return(UniTaskRentArray rentArray)
        {
            if(rentArray.TryExtractArray16(out var array16)) {
                // The array must be cleared because UniTask contains a reference type field.
                array16.AsSpan().Clear();
                ChainInstancePool<UniTaskArray16>.ReturnInstance(array16);
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

#if DEBUG
        [ModuleInitializer]
        [Obsolete("Don't call this method explicitly.", true)]
        internal static void __ModuleInitializer_SizeAssertionUniTaskArray16Core()
        {
            var a = new UniTaskArray16Core();
            Debug.Assert(Unsafe.AreSame(ref Unsafe.Add(ref a.E0, UniTaskArray16Core.ElementCount - 1), ref a.E15));
            Debug.Assert(Unsafe.SizeOf<UniTaskArray16Core>() == Unsafe.SizeOf<UniTask>() * UniTaskArray16Core.ElementCount);
        }
#endif

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
    }

    internal abstract class UniTaskArray
    {
        public abstract int Length { get; }
    }

    public readonly struct UniTaskRentArray
    {
        private readonly object? _obj;
        private readonly int _length;

        internal static UniTaskRentArray Empty => default;

        [Obsolete("Don't use default constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public UniTaskRentArray() => throw new NotSupportedException("Don't use default constructor.");

        internal UniTaskRentArray(UniTaskMemoryPool.UniTaskArray16 array16, int length)
        {
            _obj = array16;
            _length = length;
        }

        internal UniTaskRentArray(UniTask[] array)
        {
            _obj = array;
            _length = array.Length;
        }

        internal bool TryExtractArray16([MaybeNullWhen(false)] out UniTaskMemoryPool.UniTaskArray16 array16)
        {
            if(_obj is UniTaskMemoryPool.UniTaskArray16 a) {
                array16 = a;
                return true;
            }
            else {
                array16 = default;
                return false;
            }
        }

        internal bool TryExtractArray([MaybeNullWhen(false)] out UniTask[] array)
        {
            if(_obj is UniTask[] a) {
                array = a;
                return true;
            }
            else {
                array = default;
                return false;
            }
        }

        public Span<UniTask> AsSpan()
        {
            var obj = _obj;
            if(obj is null) {
                return Span<UniTask>.Empty;
            }
            else if(obj is UniTaskMemoryPool.UniTaskArray16 array16) {
                return array16.AsSpan(0, _length);
            }
            else {
                var array = SafeCast.As<UniTask[]>(obj);
                Debug.Assert(array.Length == _length);
                return array.AsSpan();
            }
        }
    }
}
