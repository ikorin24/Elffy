#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy.Threading
{
    [DebuggerDisplay("{DebuggerView,nq}")]
    [DebuggerTypeProxy(typeof(UniTaskRentArrayTypeProxy))]
    public readonly struct UniTaskRentArray
    {
        private readonly object? _obj;
        private readonly int _length;

        public int Length => _length;

        private string DebuggerView => $"{nameof(UniTaskRentArray)}[{_length}]";

        public ref UniTask this[int index]
        {
            get
            {
                if((uint)index >= _length) {
                    ThrowArgOutOfRange();
                    [DoesNotReturn] static void ThrowArgOutOfRange() => throw new ArgumentOutOfRangeException(nameof(index));
                }
                return ref Unsafe.Add(ref GetReference(), index);
            }
        }

        internal static UniTaskRentArray Empty => default;

        [Obsolete("Don't use default constructor.", true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public UniTaskRentArray() => throw new NotSupportedException("Don't use default constructor.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal UniTaskRentArray(UniTaskMemoryPool.UniTaskArray16 array16)
        {
            _obj = array16;
            _length = array16.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal UniTaskRentArray(UniTaskMemoryPool.UniTaskArray64 array64)
        {
            _obj = array64;
            _length = array64.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal UniTaskRentArray(UniTask[] array)
        {
            _obj = array;
            _length = array.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryExtractArray64([MaybeNullWhen(false)] out UniTaskMemoryPool.UniTaskArray64 array64)
        {
            if(_obj is UniTaskMemoryPool.UniTaskArray64 a) {
                array64 = a;
                return true;
            }
            else {
                array64 = default;
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<UniTask> AsSpan()
        {
            var obj = _obj;
            if(obj is null) {
                return Span<UniTask>.Empty;
            }
            else if(obj is UniTaskMemoryPool.UniTaskArray16 array16) {
                return array16.AsSpan(0, _length);
            }
            else if(obj is UniTaskMemoryPool.UniTaskArray64 array64) {
                return array64.AsSpan(0, _length);
            }
            else {
                var array = SafeCast.As<UniTask[]>(obj);
                Debug.Assert(array.Length == _length);
                return array.AsSpan();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<UniTask> AsSpan(int start) => AsSpan().Slice(start);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<UniTask> AsSpan(int start, int length) => AsSpan().Slice(start, length);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref UniTask GetReference()
        {
            var obj = _obj;
            if(obj is null) {
                return ref Unsafe.NullRef<UniTask>();
            }
            else if(obj is UniTaskMemoryPool.UniTaskArray16 array16) {
                return ref array16.GetReference();
            }
            else if(obj is UniTaskMemoryPool.UniTaskArray64 array64) {
                return ref array64.GetReference();
            }
            else {
                var array = SafeCast.As<UniTask[]>(obj);
                return ref MemoryMarshal.GetArrayDataReference(array);
            }
        }


        private sealed class UniTaskRentArrayTypeProxy
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private readonly UniTask[] _items;

            [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
            public UniTask[] Items => _items;

            public UniTaskRentArrayTypeProxy(UniTaskRentArray array)
            {
                _items = array.AsSpan().ToArray();
            }
        }
    }
}
