#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace Elffy.Effective
{
    /// <summary>
    /// Array class which is allocated in unmanaged memory.<para/>
    /// Only for unmanaged types. (e.g. int, float, recursive-unmanaged struct, and so on.)
    /// </summary>
    /// <typeparam name="T">type of array</typeparam>
    [DebuggerTypeProxy(typeof(UnmanagedArrayDebuggerTypeProxy<>))]
    [DebuggerDisplay("UnmanagedArray<{Type.Name}>[{Length}]")]
    public sealed class UnmanagedArray<T> : IList<T>, IReadOnlyList<T>, IList, IReadOnlyCollection<T>, IDisposable
        where T : unmanaged
    {
        private readonly int _length;
        private int _version;
        private bool _disposed;
        private bool _isFree;
        private readonly IntPtr _array;
        private readonly int _objsize;
        private Type? _objType;


        // Do not remove this property. Used in DebuggerDisplay Attribute.
        /// <summary>Get type of array elements.</summary>
        public Type Type => _objType ?? (_objType = typeof(T));

        /// <summary>Get pointer address of this array.</summary>
        public IntPtr Ptr { get { ThrowIfFree(); return _array; } }

        /// <summary>Get the specific item of specific index.</summary>
        /// <param name="i">index</param>
        /// <returns>The item of specific index</returns>
        public unsafe T this[int i]
        {
            get
            {
                if((uint)i >= (uint)_length) { throw new IndexOutOfRangeException(); }
                ThrowIfFree();
                return ((T*)_array)[i];
            }
            set
            {
                if((uint)i >= (uint)_length) { throw new IndexOutOfRangeException(); }
                ThrowIfFree();
                ((T*)_array)[i] = value;
                _version++;
            }
        }

        /// <summary>Get length of this array</summary>
        public int Length { get { ThrowIfFree(); return _length; } }

        /// <summary>Get wheater this array is readonly.</summary>
        public bool IsReadOnly => false;

        bool IList.IsReadOnly => false;

        bool IList.IsFixedSize => false;

        int ICollection<T>.Count { get { ThrowIfFree(); return _length; } }

        int IReadOnlyCollection<T>.Count { get { ThrowIfFree(); return _length; } }

        int ICollection.Count { get { ThrowIfFree(); return _length; } }

        object ICollection.SyncRoot => _syncRoot ?? (_syncRoot = new object());
        private object? _syncRoot;

        bool ICollection.IsSynchronized => false;

        object IList.this[int index] { get => this[index]; set => this[index] = (T)value; }

        /// <summary>UnmanagedArray Constructor</summary>
        /// <param name="length">Length of array</param>
        public unsafe UnmanagedArray(int length)
        {
            if(length < 0) { throw new ArgumentException(); }
            _objsize = sizeof(T);
            _array = Marshal.AllocHGlobal(length * _objsize);
            _length = length;

            // initialize all block as zero
            for(int i = 0; i < _objsize * length; i++) {
                Marshal.WriteByte(_array + i, 0x00);
            }
        }

        /// <summary>Create new <see cref="UnmanagedArray{T}"/>, those elements are copied from <see cref="Span{T}"/>.</summary>
        /// <param name="span">Elements of the <see cref="UnmanagedArray{T}"/> are initialized by this <see cref="Span{T}"/>.</param>
        public unsafe UnmanagedArray(Span<T> span)
        {
            _objsize = sizeof(T);
            _array = Marshal.AllocHGlobal(span.Length * _objsize);
            _length = span.Length;

            // initialize all block as zero
            for(int i = 0; i < span.Length * _objsize; i++) {
                Marshal.WriteByte(_array + i, 0x00);
            }

            var array = (T*)_array;
            for(int i = 0; i < _length; i++) {
                array[i] = span[i];
            }
        }

        ~UnmanagedArray() => Dispose(false);

        /// <summary>Get enumerator instance.</summary>
        /// <returns></returns>
        public Enumerator GetEnumerator()
        {
            ThrowIfFree();
            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            ThrowIfFree();
            // Avoid boxing by using class enumerator.
            return new EnumeratorClass(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            ThrowIfFree();
            // Avoid boxing by using class enumerator.
            return new EnumeratorClass(this);
        }

        /// <summary>
        /// Free the allocated memory of this instance. <para/>
        /// If already free, do nothing.<para/>
        /// </summary>
        public void Free()
        {
            if(!_isFree) {
                Marshal.FreeHGlobal(_array);
                _isFree = true;
            }
        }

        /// <summary>Get index of the item</summary>
        /// <param name="item">target item</param>
        /// <returns>index (if not contain, value is -1)</returns>
        public int IndexOf(T item)
        {
            ThrowIfFree();
            for(int i = 0; i < _length; i++) {
                if(item.Equals(this[i])) { return i; }
            }
            return -1;
        }

        /// <summary>Get whether this instance contains the item.</summary>
        /// <param name="item">target item</param>
        /// <returns>true: This array contains the target item. false: not contain</returns>
        public bool Contains(T item)
        {
            ThrowIfFree();
            for(int i = 0; i < _length; i++) {
                if(item.Equals(this[i])) { return true; }
            }
            return false;
        }

        /// <summary>Copy to managed memory</summary>
        /// <param name="array">managed memory array</param>
        /// <param name="arrayIndex">start index of destination array</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            ThrowIfFree();
            if(array == null) { throw new ArgumentNullException(nameof(array)); }
            if(arrayIndex + _length > array.Length) { throw new ArgumentException("There is not enouph length of destination array"); }
            unsafe {
                fixed(T* arrayPtr = array) {
                    var byteLen = (long)(_length * _objsize);
                    var dest = new IntPtr(arrayPtr) + arrayIndex * _objsize;
                    Buffer.MemoryCopy((void*)_array, (void*)dest, byteLen, byteLen);
                }
            }
        }

        void IList<T>.Insert(int index, T item) => throw new NotSupportedException();
        void IList<T>.RemoveAt(int index) => throw new NotSupportedException();
        void ICollection<T>.Add(T item) => throw new NotSupportedException();
        bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
        void ICollection<T>.Clear() => throw new NotSupportedException();

        /// <summary>Copy from unmanaged.</summary>
        /// <param name="source">unmanaged source pointer</param>
        /// <param name="start">start index of destination. (destination is this <see cref="UnmanagedArray{T}"/>.)</param>
        /// <param name="length">count of copied item. (NOT length of bytes.)</param>
        public unsafe void CopyFrom(IntPtr source, int start, int length)
        {
            ThrowIfFree();
            if(source == IntPtr.Zero) { throw new ArgumentNullException("source is null"); }
            if(start < 0 || length < 0) { throw new ArgumentOutOfRangeException(); }
            if(start + length > _length) { throw new ArgumentOutOfRangeException(); }
            var byteLen = (long)(length * _objsize);
            Buffer.MemoryCopy((void*)source, (void*)(_array + start * _objsize), byteLen, byteLen);
            _version++;
        }

        /// <summary>Copy from <see cref="Span{T}"/>.</summary>
        /// <param name="source"><see cref="Span{T}"/> object.</param>
        /// <param name="start">start index of destination. (destination is this <see cref="UnmanagedArray{T}"/>.)</param>
        public unsafe void CopyFrom(Span<T> source, int start)
        {
            ThrowIfFree();
            if(start < 0) { throw new ArgumentOutOfRangeException(); }
            if(start + source.Length > _length) { throw new ArgumentOutOfRangeException(); }
            fixed(T* ptr = source) {
                var byteLen = (long)(source.Length * _objsize);
                Buffer.MemoryCopy(ptr, (void*)(_array + start * _objsize), byteLen, byteLen);
                _version++;
            }
        }

        /// <summary>Return <see cref="Span{T}"/> of this <see cref="UnmanagedArray{T}"/>.</summary>
        /// <returns></returns>
        public unsafe Span<T> AsSpan()
        {
            ThrowIfFree();
            return new Span<T>((T*)_array, _length);
        }

        /// <summary>Create new <see cref="UnmanagedArray{T}"/> whose values are initialized by memory layout of specified structure.</summary>
        /// <typeparam name="TStruct">type of source structure</typeparam>
        /// <param name="obj">source structure</param>
        /// <returns>instance of <see cref="UnmanagedArray{T}"/> whose values are initialized by <paramref name="obj"/></returns>
        public static unsafe UnmanagedArray<T> CreateFromStruct<TStruct>(TStruct obj) where TStruct : unmanaged
        {
            var structSize = sizeof(TStruct);
            var itemSize = sizeof(T);
            var arrayLen = structSize / itemSize + (structSize % itemSize > 0 ? 1 : 0);
            var array = new UnmanagedArray<T>(arrayLen);
            Buffer.MemoryCopy(&obj, (void*)array._array, structSize, structSize);
            return array;
        }

        /// <summary>Dispose this instance and release unmanaged memory.</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if(_disposed) { return; }
            if(disposing) {
                // relase managed resource here.
            }
            Free();
            _disposed = true;
        }

        private void ThrowIfFree()
        {
            if(_isFree) { throw new InvalidOperationException("Memory of Array is already free."); }
        }

        int IList.Add(object value) => throw new NotSupportedException();
        void IList.Clear() => throw new NotSupportedException();
        void IList.Insert(int index, object value) => throw new NotSupportedException();
        void IList.Remove(object value) => throw new NotSupportedException();
        void IList.RemoveAt(int index) => throw new NotSupportedException();
        bool IList.Contains(object value) => (value is T v) ? Contains(v) : false;
        int IList.IndexOf(object value) => (value is T v) ? IndexOf(v) : -1;
        void ICollection.CopyTo(Array array, int index) => CopyTo((T[])array, index);

        [Serializable]
        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly UnmanagedArray<T> _array;
            private readonly int _version;
            private int _index;
            public T Current { get; private set; }

            internal Enumerator(UnmanagedArray<T> array)
            {
                _array = array;
                _index = 0;
                _version = _array._version;
                Current = default;
            }

            public void Dispose() { }

            public bool MoveNext()
            {
                var localArray = _array;
                if(_version == localArray._version && ((uint)_index < (uint)localArray._length)) {
                    Current = localArray[_index];
                    _index++;
                    return true;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                var localArray = _array;
                if(_version != localArray._version) {
                    throw new InvalidOperationException();
                }
                _index = localArray._length + 1;
                Current = default;
                return false;
            }

            object IEnumerator.Current
            {
                get {
                    if(_index == 0 || _index == _array._length + 1) {
                        throw new InvalidOperationException();
                    }
                    return Current;
                }
            }

            void IEnumerator.Reset()
            {
                if(_version != _array._version) {
                    throw new InvalidOperationException();
                }
                _index = 0;
                Current = default;
            }
        }

        [Serializable]
        public class EnumeratorClass : IEnumerator<T>, IEnumerator
        {
            private readonly UnmanagedArray<T> _array;
            private readonly int _version;
            private int _index;
            public T Current { get; private set; }

            internal EnumeratorClass(UnmanagedArray<T> array)
            {
                _array = array;
                _index = 0;
                _version = _array._version;
                Current = default;
            }

            public void Dispose() { }

            public bool MoveNext()
            {
                var localArray = _array;
                if(_version == localArray._version && ((uint)_index < (uint)localArray._length)) {
                    Current = localArray[_index];
                    _index++;
                    return true;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                var localArray = _array;
                if(_version != localArray._version) {
                    throw new InvalidOperationException();
                }
                _index = localArray._length + 1;
                Current = default;
                return false;
            }

            object IEnumerator.Current
            {
                get
                {
                    if(_index == 0 || _index == _array._length + 1) {
                        throw new InvalidOperationException();
                    }
                    return Current;
                }
            }

            void IEnumerator.Reset()
            {
                if(_version != _array._version) {
                    throw new InvalidOperationException();
                }
                _index = 0;
                Current = default;
            }
        }
    }

    public static class UnmanagedArrayExtension
    {
        /// <summary>Create a new instance of <see cref="UnmanagedArray{T}"/> initialized by source.</summary>
        /// <typeparam name="T">Type of item in array</typeparam>
        /// <param name="source">source which initializes new array.</param>
        /// <returns>instance of <see cref="UnmanagedArray{T}"/></returns>
        public static UnmanagedArray<T> ToUnmanagedArray<T>(this IEnumerable<T> source) where T : unmanaged 
        {
            if(source == null) { throw new ArgumentNullException(); }
            var managedArray = (source is T[] a) ? a : source.ToArray();
            var len = managedArray.Length;
            var array = new UnmanagedArray<T>(len);
            unsafe {
                fixed (T* ptr = managedArray) {
                    array.CopyFrom((IntPtr)ptr, 0, len);
                }
            }
            return array;
        }
    }

    internal class UnmanagedArrayDebuggerTypeProxy<T> where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private UnmanagedArray<T> _entity;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get {
                var items = new T[_entity.Length];
                _entity.CopyTo(items, 0);
                return items;
            }
        }

        public bool IsReadOnly => _entity.IsReadOnly;

        public UnmanagedArrayDebuggerTypeProxy(UnmanagedArray<T> entity) => _entity = entity;
    }
}
