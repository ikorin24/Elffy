#nullable enable
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Elffy.Effective
{
    public unsafe struct UnsafeRawList<T> : IDisposable where T : unmanaged
    {
        private UnsafeRawArray<T> _array;
        private int _count;

        public readonly int Count => _count;

        public readonly int Capacity => _array.Length;

        public readonly IntPtr Ptr => _array.Ptr;

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref _array[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UnsafeRawList(int capacity)
        {
            _array = new UnsafeRawArray<T>(capacity, true);
            _count = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(in T item)
        {
            if(_count >= _array.Length) {
                Extend();
            }
            _array[_count] = item;
            _count++;
        }

        public int IndexOf(in T item)
        {
            for(int i = 0; i < _count; i++) {
                if(EqualityComparer<T>.Default.Equals(_array[i], item)) {
                    return i;
                }
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]  // uncommon path
        private void Extend()
        {
            var newArray = new UnsafeRawArray<T>(_array.Length == 0 ? 4 : _array.Length * 2);
            try {
                Buffer.MemoryCopy(_array.Ptr.ToPointer(), newArray.Ptr.ToPointer(), newArray.Length * sizeof(T), _array.Length * sizeof(T));
            }
            catch {
                newArray.Dispose();
                throw;
            }
            _array.Dispose();
            _array = newArray;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            _array.Dispose();
            _count = 0;
        }
    }
}
