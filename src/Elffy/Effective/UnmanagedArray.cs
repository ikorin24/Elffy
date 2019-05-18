using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elffy.Effective
{
    /// <summary>
    /// Array class which is allocated in unmanaged memory.<para/>
    /// Only for unmanaged type. (e.g. int, float, struct, and so on.)
    /// </summary>
    /// <typeparam name="T">type of array</typeparam>
    [DebuggerDisplay("Length = {Length}")]
    public sealed class UnmanagedArray<T> : IList<T>, IReadOnlyList<T>, IDisposable
        where T : unmanaged
    {
        #region private member
        private readonly object _syncRoot = new object();
        private int _version;
        private bool _disposed;
        private bool _isFree;
        private readonly IntPtr _array;
        private readonly int _objsize;
        #endregion private member

        public T this[int i]
        {
            get
            {
                ThrowIfFree();
                unsafe {
                    return *(T*)(_array + i * _objsize);
                }
            }
            set
            {
                ThrowIfFree();
                var ptr = _array + i * _objsize;
                Marshal.StructureToPtr<T>(value, ptr, true);
                _version++;
            }
        }

        /// <summary>Length of this array</summary>
        public int Length { get; private set; }

        /// <summary>Length of this array (ICollection implementation)</summary>
        public int Count => Length;

        public bool IsReadOnly => false;

        #region constructor
        /// <summary>UnmanagedArray Constructor</summary>
        /// <param name="length">Length of array</param>
        public UnmanagedArray(int length)
        {
            if(length < 0) { throw new InvalidOperationException(); }
            _objsize = Marshal.SizeOf<T>();
            _array = Marshal.AllocHGlobal(length * _objsize);
            Length = length;
        }

        ~UnmanagedArray() => Dispose(false);
        #endregion

        #region Free
        /// <summary>Free the allocated memory of this instance.</summary>
        public void Free()
        {
            lock(_syncRoot) {
                if(!_isFree) {
                    Marshal.FreeHGlobal(_array);
                    _isFree = true;
                }
            }
        }
        #endregion

        #region IList implementation
        /// <summary>Get index of the item</summary>
        /// <param name="item">target item</param>
        /// <returns>index (if not contain, value is -1)</returns>
        public int IndexOf(T item)
        {
            for(int i = 0; i < Length; i++) {
                if(item.Equals(this[i])) { return i; }
            }
            return -1;
        }

        /// <summary>Get whether this instance contains the item.</summary>
        /// <param name="item">target item</param>
        /// <returns>true: This array contains the target item. false: not contain</returns>
        public bool Contains(T item)
        {
            for(int i = 0; i < Length; i++) {
                if(item.Equals(this[i])) { return true; }
            }
            return false;
        }

        /// <summary>Copy to managed memory</summary>
        /// <param name="array">managed memory array</param>
        /// <param name="arrayIndex">start index of destination array</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if(array == null) { throw new ArgumentNullException(nameof(array)); }
            if(arrayIndex + Length > array.Length) { throw new ArgumentException("There is not enouph length of array"); }
            unsafe {
                for(int i = 0; i < Length; i++) {
                    array[i + arrayIndex] = this[i];
                }
            }
        }

        public IEnumerator<T> GetEnumerator() => new Enumerator(this);

        /// <summary>Not Supported in this class.</summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, T item) => throw new NotSupportedException();

        /// <summary>Not Supported in this class.</summary>
        /// <param name="index"></param>
        public void RemoveAt(int index) => throw new NotSupportedException();

        /// <summary>Not Supported in this class.</summary>
        /// <param name="item"></param>
        public void Add(T item) => throw new NotSupportedException();

        /// <summary>Not Supported in this class.</summary>
        public bool Remove(T item) => throw new NotSupportedException();

        /// <summary>Not Supported in this class.</summary>
        public void Clear() => throw new NotSupportedException();
        #endregion

        #region Dispose
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
        #endregion

        private void ThrowIfFree()
        {
            if(_isFree) { throw new InvalidOperationException("Memory of Array is already free."); }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #region struct Enumerator
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
                var localList = _array;
                if(_version == localList._version && ((uint)_index < (uint)localList.Length)) {
                    Current = localList[_index];
                    _index++;
                    return true;
                }

                if(_version != _array._version) {
                    throw new InvalidOperationException();
                }
                _index = _array.Length + 1;
                Current = default;
                return false;
            }

            object IEnumerator.Current
            {
                get {
                    if(_index == 0 || _index == _array.Length + 1) {
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
        #endregion struct Enumerator
    }
}
