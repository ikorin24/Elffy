#nullable enable
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Elffy.Effective.Unsafes;

namespace Elffy.Features
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct ArraySliceEnumerator<T> : IEnumerator<T>, IEnumerator
    {
        private readonly T[]? _array;
        private readonly int _count;
        private int _i;
        private T _current;
        public T Current => _current;

        object IEnumerator.Current => _current!;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal ArraySliceEnumerator(T[]? array, int count)
        {
            Debug.Assert((array is null && count == 0) || (array is not null));     // count must be 0 when array is null.

            _array = array;
            _count = count;
            _i = 0;
            _current = default!;
        }

        public void Dispose() { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            // [NOTE]
            // _count == 0 when _array is null, so the method always returns false.

            if(_i < _count) {
                Debug.Assert(_array is not null);
                _current = _array.At(_i);
                _i++;
                return true;
            }
            else {
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset()
        {
            _i = 0;
            _current = default!;
        }
    }
}
