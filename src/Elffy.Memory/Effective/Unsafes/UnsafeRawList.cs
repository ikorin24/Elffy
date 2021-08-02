#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Elffy.Effective.Unsafes
{
    /// <summary>Provides list which is allocated in unmanaged memory.</summary>
    /// <remarks>
    /// 1) DO NOT create a default instance (<see langword="new"/> <see cref="UnsafeRawList{T}"/>(), or <see langword="default"/>).
    ///    That means <see langword="null"/> for reference types.<para/>
    ///    Use <see cref="New()"/> instead.<para/>
    /// 2) You MUST call <see cref="Dispose"/> after use it. Or it causes MEMORY LEAK !<para/>
    /// 3) It DOES NOT check any boundary of access by index.<para/>
    /// </remarks>
    /// <typeparam name="T">element type</typeparam>
    [DebuggerTypeProxy(typeof(UnsafeRawListDebuggerTypeProxy<>))]
    [DebuggerDisplay("{DebugView,nq}")]
    public unsafe readonly struct UnsafeRawList<T> : IDisposable, IEquatable<UnsafeRawList<T>> where T : unmanaged
    {
        // =============================================================
        // new UnsafeRawList<T>(n)   (n > 0)
        //
        //   UnsafeRawList<T>
        //   +--------------+   ┌- ref CountRef()
        //   |    IntPtr    |   |          ref ArrayRef()           ref GetReference()
        //   | 4 or 8 bytes |   |          |                         |
        //   |    _ptr      |   |          |                         |
        //   +----|---------+   ↓         ↓   on unmanaged memory  |
        //        |           +---------+----------+--------------+  |
        //        |           |   int   |    UnsafeRawArray<T>    |  |
        //        |           |         |   int    |    IntPtr    |  |
        //        `---------> | 4 bytes | 4 bytes  | 4 or 8 bytes |  |
        //                    |  Count  | Capacity |     Ptr      |  |
        //                    +---------+----------+---|----------+  ↓    on unmanaged memory
        //                                             |    +-----------------+----
        //                                             |    |        T        | ... 
        //                                             `--> | sizeof(T) bytes | ... 
        //                                                  |     item[0]     | ... 
        //                                                  +-----------------+----
        // default(UnsafeRawList<T>)   (That means null)
        //
        // _ptr == IntPtr.Zero
        // =============================================================

        private readonly IntPtr _ptr;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebugView => _ptr == IntPtr.Zero ? "null" : $"UnsafeRawList<{typeof(T).Name}>[{CountRef()}]";

        public bool IsNull => _ptr == IntPtr.Zero;

        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => CountRef();
        }

        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ArrayRef().Length;
            set
            {
                var count = CountRef();
                if(value < count) {
                    ThrowOutOfRange(nameof(value));
                }
                ref var array = ref ArrayRef();
                if(value == array.Length) { return; }
                Debug.Assert(value > array.Length);
                var newArray = new UnsafeRawArray<T>(value, false);
                try {
                    if(count > 0) {
                        array.AsSpan(0, count).CopyTo(newArray.AsSpan());
                    }
                }
                catch {
                    newArray.Dispose();
                    throw;
                }
                array.Dispose();
                array = newArray;
            }
        }

        /// <summary>Get pointer to the head</summary>
        public readonly IntPtr Ptr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ArrayRef().Ptr;
        }

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref ArrayRef()[index];
        }

        public static UnsafeRawList<T> Null => default;

        public static UnsafeRawList<T> New() => new UnsafeRawList<T>(4);

        public static UnsafeRawList<T> New(int capacity) => new UnsafeRawList<T>(capacity);

        public static UnsafeRawList<T> New(ReadOnlySpan<T> collection) => new UnsafeRawList<T>(collection);

        private UnsafeRawList(int capacity)
        {
            if(capacity < 0) {
                ThrowOutOfRange(nameof(capacity));
            }
            _ptr = Marshal.AllocHGlobal(sizeof(int) + sizeof(UnsafeRawArray<T>));
            CountRef() = 0;
            ArrayRef() = new UnsafeRawArray<T>(capacity);
        }

        private UnsafeRawList(ReadOnlySpan<T> collection)
        {
            _ptr = Marshal.AllocHGlobal(sizeof(int) + sizeof(UnsafeRawArray<T>));
            CountRef() = collection.Length;
            ref var array = ref ArrayRef();
            array = new UnsafeRawArray<T>(collection.Length);
            collection.CopyTo(array.AsSpan());
        }

        /// <summary>Get reference to the 0th element of the list. (If <see cref="Capacity"/> is 0, returns reference to null)</summary>
        /// <returns>reference</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetReference() => ref ArrayRef().GetReference();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(in T item)
        {
            ref var array = ref ArrayRef();
            ref var count = ref CountRef();
            if(count >= array.Length) {
                ExtendCapcityToNextSize();   // Never inlined for performance because uncommon path.
                Debug.Assert(count < array.Length);
            }
            array[count] = item;
            count++;
        }

        public void AddRange(ReadOnlySpan<T> collection)
        {
            // Get array reference at first. NullReferenceException is thrown if 'this' is null.
            ref var array = ref ArrayRef();
            if(collection.IsEmpty) { return; }

            ref var count = ref CountRef();
            if(count + collection.Length >= array.Length) {
                var newArray = new UnsafeRawArray<T>(count + collection.Length);
                try {
                    array.AsSpan(0, count).CopyTo(newArray.AsSpan());
                }
                catch {
                    newArray.Dispose();
                    throw;
                }
                array.Dispose();
                array = newArray;
            }
            collection.CopyTo(array.AsSpan(count));
            count += collection.Length;
        }

        public int IndexOf(T item)
        {
            var count = CountRef();
            var array = ArrayRef();
            for(int i = 0; i < count; i++) {
                if(EqualityComparer<T>.Default.Equals(array[i], item)) {
                    return i;
                }
            }
            return -1;
        }

        public void RemoveAt(int index)
        {
            ref var count = ref CountRef();
            count--;
            if(index < count) {
                ref var array = ref ArrayRef();
                array.AsSpan(index + 1).CopyTo(array.AsSpan(index));
            }
        }

        public bool Remove(in T item)
        {
            var index = IndexOf(item);
            if(index <= 0) {
                RemoveAt(index);
                return true;
            }
            else {
                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            CountRef() = 0;
        }

        public Span<T> Extend(int count, bool zeroFill = false)
        {
            // Get array reference at first. NullReferenceException is thrown if 'this' is null.
            ref readonly var array = ref ArrayRef();
            if(count < 0) { ThrowOutOfRange(nameof(count)); }
            if(count == 0) { return Span<T>.Empty; }

            ref var itemCount = ref CountRef();

            var margin = array.Length - itemCount;
            if(margin < count) {
                Capacity += count - margin;
            }
            var newSpan = array.AsSpan(itemCount, count);
            itemCount += count;
            if(zeroFill) {
                newSpan.Clear();
            }
            return newSpan;
        }


        /// <summary>Copy elements to a new array.</summary>
        /// <returns>array</returns>
        public T[] ToArray()
        {
            return AsSpan().ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan()
        {
            if(_ptr == IntPtr.Zero) {
                return Span<T>.Empty;
            }
            return ArrayRef().AsSpan(0, CountRef());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan(int start)
        {
            if(_ptr == IntPtr.Zero) {
                if(start == 0) {
                    return Span<T>.Empty;
                }
                ThrowOutOfRange(nameof(start));
            }
            return ArrayRef().AsSpan(start, CountRef() - start);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> AsSpan(int start, int length)
        {
            if(_ptr == IntPtr.Zero) {
                if(start == 0 && length == 0) {
                    return Span<T>.Empty;
                }
                ThrowOutOfRange(nameof(start));
            }
            return ArrayRef().AsSpan(start, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            if(_ptr == IntPtr.Zero) { return; }
            ArrayRef().Dispose();
            Marshal.FreeHGlobal(_ptr);
            Unsafe.AsRef(_ptr) = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T* GetPtr()
        {
            return ArrayRef().GetPtr();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]  // uncommon path
        private void ExtendCapcityToNextSize()
        {
            Debug.Assert(_ptr != IntPtr.Zero);

            // Double the capacity. If the current capacity is 0, set it to 4.

            var size = ArrayRef().Length;
            if(size * 2 < 0) {  // overflow int
                size = int.MaxValue;
            }
            else {
                size = Math.Max(4, size * 2);
            }
            Capacity = size;
        }

        private ref int CountRef() => ref *(int*)_ptr;
        private ref UnsafeRawArray<T> ArrayRef() => ref *(UnsafeRawArray<T>*)(((int*)_ptr) + 1);

        public override string? ToString()
        {
            if(_ptr == IntPtr.Zero) { throw new NullReferenceException(); }
            return base.ToString();
        }

        public override bool Equals(object? obj) => obj is UnsafeRawList<T> list && Equals(list);

        public bool Equals(UnsafeRawList<T> other) => _ptr == other._ptr;

        public override int GetHashCode() => _ptr.GetHashCode();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(UnsafeRawList<T> left, UnsafeRawList<T> right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(UnsafeRawList<T> left, UnsafeRawList<T> right) => !(left == right);

        [DoesNotReturn]
        private static void ThrowOutOfRange(string message) => throw new ArgumentOutOfRangeException(message);
    }

    internal class UnsafeRawListDebuggerTypeProxy<T> where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly UnsafeRawList<T> _entity;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => _entity.AsSpan().ToArray();

        public UnsafeRawListDebuggerTypeProxy(UnsafeRawList<T> entity) => _entity = entity;
    }
}
