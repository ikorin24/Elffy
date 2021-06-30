#nullable enable
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Elffy.Effective
{
    /// <summary>Provides array type where references to the items are weak reference</summary>
    /// <typeparam name="T"></typeparam>
    [DebuggerDisplay("{typeof(T).Name,nq}[{Length}] (HandleType={_handleType})")]
    [DebuggerTypeProxy(typeof(WeakReferenceArrayDebuggerTypeProxy<>))]
    public sealed class WeakReferenceArray<T> :
        IDisposable, ICollection<T?>, IEnumerable<T?>, IEnumerable, IList<T?>, IReadOnlyCollection<T?>, IReadOnlyList<T?>, ICollection, IList
        where T : class?
    {
        private Element[] _array;
        private readonly GCHandleType _handleType;

        private static WeakReferenceArray<T?>? _empty;

        /// <summary>Get empty instance of <see cref="WeakReferenceArray{T}"/></summary>
        public static WeakReferenceArray<T?> Empty => _empty ??= new(0);

        /// <summary>Get length of the array. (The length get zero when called <see cref="Dispose"/>)</summary>
        public int Length => _array.Length;

        int ICollection<T?>.Count => Length;

        bool ICollection<T?>.IsReadOnly => false;

        int IReadOnlyCollection<T?>.Count => Length;

        int ICollection.Count => Length;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        bool IList.IsFixedSize => true;

        bool IList.IsReadOnly => false;

        object? IList.this[int index] { get => this[index]; set => this[index] = (T?)value; }

        /// <summary>Create a new array of specified length where references to the items are weak reference.</summary>
        /// <param name="length">length of the array</param>
        /// <param name="trackResurrection"><see langword="true"/> if you use <see cref="GCHandleType.WeakTrackResurrection"/>, otherwise <see cref="GCHandleType.Weak"/></param>
        public WeakReferenceArray(int length, bool trackResurrection = false)
        {
            _handleType = trackResurrection ? GCHandleType.WeakTrackResurrection : GCHandleType.Weak;
            _array = new Element[length];
        }

        /// <summary>Create a new array initialized by the specified <paramref name="collection"/>, where references to the items are weak reference.</summary>
        /// <param name="collection"></param>
        /// <param name="trackResurrection"></param>
        public WeakReferenceArray(IEnumerable<T?> collection, bool trackResurrection = false)
        {
            var handleType = trackResurrection ? GCHandleType.WeakTrackResurrection : GCHandleType.Weak;
            var len = collection switch
            {
                ICollection<T?> c => c.Count,
                ICollection c => c.Count,
                IReadOnlyCollection<T?> c => c.Count,
                IEnumerable<T?> e => e.Count(),
                _ => throw new ArgumentNullException(nameof(collection)),
            };
            var array = new Element[len];
            int i = 0;
            foreach(var item in collection) {
                array[i++].SetValue(item, handleType);
            }
            _handleType = handleType;
            _array = array;
        }

        ~WeakReferenceArray() => DisposePrivate();

        public T? this[int index]
        {
            get
            {
                if((uint)index >= (uint)_array.Length) { ThrowOutOfRange(); }
                return _array[index].GetValue();
            }
            set
            {
                if((uint)index >= (uint)_array.Length) { ThrowOutOfRange(); }
                _array[index].SetValue(value, _handleType);
            }
        }

        /// <summary>Free all references to the items</summary>
        /// <remarks><see cref="Length"/> get zero when calling this method,.</remarks>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            DisposePrivate();
        }

        public void DisposePrivate()
        {
            var array = _array;
            _array = Array.Empty<Element>();
            foreach(var item in array) {
                item.Free();
            }
        }

        public Enumerator GetEnumerator() => new(this);

        IEnumerator<T?> IEnumerable<T?>.GetEnumerator() => new EnumeratorClass(this);

        IEnumerator IEnumerable.GetEnumerator() => new EnumeratorClass(this);

        void ICollection<T?>.Add(T? item) => throw new NotSupportedException();

        void ICollection<T?>.Clear() => throw new NotSupportedException();

        bool ICollection<T?>.Contains(T? item)
        {
            var array = _array;
            var comparer = EqualityComparer<T?>.Default;
            for(int i = 0; i < array.Length; i++) {
                if(comparer.Equals(array[i].GetValue(), item)) {
                    return true;
                }
            }
            return false;
        }

        public void CopyTo(T?[] array, int arrayIndex)
        {
            if(array == null) { throw new ArgumentNullException(nameof(array)); }
            if((uint)arrayIndex >= (uint)array.Length) { throw new ArgumentOutOfRangeException(nameof(arrayIndex)); }
            if(arrayIndex + Length > array.Length) { throw new ArgumentException("There is not enouph length of destination array"); }

            var self = _array;
            for(int i = 0; i < self.Length; i++) {
                array[arrayIndex + i] = self[i].GetValue();
            }
        }

        bool ICollection<T?>.Remove(T? item) => throw new NotSupportedException();

        int IList<T?>.IndexOf(T? item)
        {
            var array = _array;
            var comparer = EqualityComparer<T?>.Default;
            for(int i = 0; i < array.Length; i++) {
                if(comparer.Equals(array[i].GetValue(), item)) {
                    return i;
                }
            }
            return -1;
        }

        void IList<T?>.Insert(int index, T? item) => throw new NotSupportedException();

        void IList<T?>.RemoveAt(int index) => throw new NotSupportedException();

        void ICollection.CopyTo(Array array, int index) => ((ICollection<T?>)this).CopyTo((T?[])array, index);

        int IList.Add(object? value) => throw new NotSupportedException();

        void IList.Clear() => throw new NotSupportedException();

        bool IList.Contains(object? value) => (value is T v) && ((IList<T?>)this).Contains(v);

        int IList.IndexOf(object? value) => (value is T v) ? ((IList<T?>)this).IndexOf(v) : -1;

        void IList.Insert(int index, object? value) => throw new NotSupportedException();

        void IList.Remove(object? value) => throw new NotSupportedException();

        void IList.RemoveAt(int index) => throw new NotSupportedException();

        [DoesNotReturn]
        private static void ThrowOutOfRange() => throw new ArgumentOutOfRangeException("index");

        private struct Element
        {
            public GCHandle _handle;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T? GetValue()
            {
                return _handle.IsAllocated ? Unsafe.As<T?>(_handle.Target) : null;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetValue(T? value, GCHandleType handleType)
            {
                Free();
                _handle = GCHandle.Alloc(value, handleType);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Free()
            {
                if(_handle.IsAllocated) {
                    _handle.Free();
                }
            }
        }

        private sealed class EnumeratorClass : IEnumerator<T?>
        {
            private Enumerator _e;  // mutable object. Don't make it 'readonly'

            public T? Current => _e.Current;

            object? IEnumerator.Current => _e.Current;

            public void Dispose() => _e.Dispose();

            public bool MoveNext() => _e.MoveNext();

            public void Reset() => _e.Reset();

            public EnumeratorClass(WeakReferenceArray<T?> array)
            {
                _e = new(array);
            }
        }

        public struct Enumerator : IEnumerator<T?>
        {
            private readonly WeakReferenceArray<T?> _array;
            private T? _current;
            private int _index;

            internal Enumerator(WeakReferenceArray<T?> array)
            {
                _array = array;
                _current = default;
                _index = 0;
            }

            public T? Current => _current;

            object? IEnumerator.Current => _current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if((uint)_index >= (uint)_array.Length) { return false; }
                _current = _array[_index++];
                return true;
            }

            public void Reset()
            {
                _current = default;
                _index = 0;
            }
        }
    }

    internal sealed class WeakReferenceArrayDebuggerTypeProxy<T> where T : class?
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly WeakReferenceArray<T?> _entity;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T?[] Items
        {
            get
            {
                var array = new T?[_entity.Length];
                _entity.CopyTo(array, 0);
                return array;
            }
        }

        public WeakReferenceArrayDebuggerTypeProxy(WeakReferenceArray<T?> entity)
        {
            _entity = entity;
        }
    }
}
