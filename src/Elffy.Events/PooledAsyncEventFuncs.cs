#nullable enable
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy
{
    internal unsafe readonly struct PooledAsyncEventFuncs<T> where T : class
    {
        private readonly object?[] _array;
        private readonly int _start;
        private readonly int _count;
        private readonly delegate*<in PooledAsyncEventFuncs<T>, void> _returnFunc;

        public int Count => _count;

        public T? this[int index]
        {
            get
            {
                if((uint)index >= (uint)_count) { ThrowOutOfRange(); }
                Debug.Assert(_array is not null);
                return Unsafe.As<T>(_array[index]);

                [DoesNotReturn] static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public PooledAsyncEventFuncs(int count)
        {
            if(count <= ArrayPoolForEventSource.LengthOfPoolTargetArray) {
                if(ArrayPoolForEventSource.TryGetArray4(out var array)) {
                    _array = array;
                    _start = 0;
                    _count = count;
                    _returnFunc = &ReturnArrayPoolForEventSource;
                    return;
                }
            }

            // TODO: instance pooling
            _array = new object[count];
            _start = 0;
            _count = count;
            _returnFunc = null;

            static void ReturnArrayPoolForEventSource(in PooledAsyncEventFuncs<T> funcs)
            {
                var array = funcs._array;
                Debug.Assert(array is not null);
                Debug.Assert(array.Length == ArrayPoolForEventSource.LengthOfPoolTargetArray);
                Debug.Assert(array.Length == 4);
                array[3] = null;
                array[0] = null;
                array[1] = null;
                array[2] = null;
                ArrayPoolForEventSource.ReturnArray4Fast(array);
            }
        }

        public Span<T> AsSpan()
        {
            if(_array == null) {
                return Span<T>.Empty;
            }
            ref var head = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(_array), _start);
            return MemoryMarshal.CreateSpan(ref Unsafe.As<object?, T>(ref head), _count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return()
        {
            var returnFunc = _returnFunc;
            if(returnFunc != null) {
                returnFunc(this);
            }
        }
    }
}
