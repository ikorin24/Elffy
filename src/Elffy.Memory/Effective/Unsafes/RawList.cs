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
    /// 1) DO NOT create a default instance (<see langword="new"/> <see cref="RawList{T}"/>(), or <see langword="default"/>).
    ///    That means <see langword="null"/> for reference types.<para/>
    ///    Use <see cref="New"/> instead.<para/>
    /// 2) You MUST call <see cref="Dispose"/> after use it. Or it causes MEMORY LEAK !<para/>
    /// 3) It DOES NOT check any boundary of access by index.<para/>
    /// </remarks>
    /// <typeparam name="T">element type</typeparam>
    [DebuggerTypeProxy(typeof(RawListDebuggerTypeProxy<>))]
    [DebuggerDisplay("{DebugView,nq}")]
    public unsafe readonly struct RawList<T> : IDisposable, IEquatable<RawList<T>> where T : unmanaged
    {
        // =============================================================
        // new UnsafeRawList<T>(n)   (n > 0)
        //
        //   UnsafeRawList<T>
        //   +--------------+
        //   |    IntPtr    |
        //   | 4 or 8 bytes |
        //   |    _ptr      |
        //   +----|---------+
        //        |    +---------+----------+--------------+
        //        |    |   int   |    UnsafeRawArray<T>    |
        //        |    |         |   int    |    IntPtr    |
        //        `--> | 4 bytes | 4 bytes  | 4 or 8 bytes |
        //             |  Count  | Capacity |  (array ptr) |
        //             +---------+----------+---|----------+
        //                                      |    +-----------------+----
        //                                      |    |        T        | ... 
        //                                      `--> | sizeof(T) bytes | ... 
        //                                           |     item[0]     | ... 
        //                                           +-----------------+----
        // default(UnsafeRawList<T>)   (That means null)
        //
        // _ptr == IntPtr.Zero
        // =============================================================

        public static RawList<T> Null => default;

        private readonly IntPtr _ptr;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private string DebugView => _ptr == IntPtr.Zero ? "null" : $"UnsafeRawList<{typeof(T).Name}>[{CountRef()}]";

        public int Count
        {
            get
            {
                if(_ptr == IntPtr.Zero) { ThrowNullRef(); }
                return CountRef();
            }
        }

        public int Capacity
        {
            get
            {
                if(_ptr == IntPtr.Zero) { ThrowNullRef(); }
                return ArrayRef().Length;
            }
            set
            {
                if(_ptr == IntPtr.Zero) { ThrowNullRef(); }
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
            get
            {
                if(_ptr == IntPtr.Zero) { ThrowNullRef(); }
                return ArrayRef().Ptr;
            }
        }

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if(_ptr == IntPtr.Zero) { ThrowNullRef(); }
                return ref ArrayRef()[index];
            }
        }

        public static RawList<T> New() => new RawList<T>(4);

        public static RawList<T> New(int capacity) => new RawList<T>(capacity);

        public static RawList<T> New(ReadOnlySpan<T> collection) => new RawList<T>(collection);

        private RawList(int capacity)
        {
            if(capacity < 0) {
                ThrowOutOfRange(nameof(capacity));
            }
            _ptr = Marshal.AllocHGlobal(sizeof(int) + sizeof(UnsafeRawArray<T>));
            CountRef() = 0;
            ArrayRef() = new UnsafeRawArray<T>(capacity);
        }

        private RawList(ReadOnlySpan<T> collection)
        {
            _ptr = Marshal.AllocHGlobal(sizeof(int) + sizeof(UnsafeRawArray<T>));
            CountRef() = collection.Length;
            ref var array = ref ArrayRef();
            array = new UnsafeRawArray<T>(collection.Length);
            collection.CopyTo(array.AsSpan());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(in T item)
        {
            if(_ptr == IntPtr.Zero) { ThrowNullRef(); }
            ref var array = ref ArrayRef();
            ref var count = ref CountRef();
            if(count >= array.Length) {
                Extend();   // Never inlined for performance because uncommon path.
                Debug.Assert(count < array.Length);
            }
            array[count] = item;
            count++;
        }

        public void AddRange(ReadOnlySpan<T> collection)
        {
            if(_ptr == IntPtr.Zero) { ThrowNullRef(); }
            if(collection.IsEmpty) { return; }
            ref var array = ref ArrayRef();
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
            if(_ptr == IntPtr.Zero) { ThrowNullRef(); }
            var count = CountRef();
            var array = ArrayRef();
            for(int i = 0; i < count; i++) {
                if(EqualityComparer<T>.Default.Equals(array[i], item)) {
                    return i;
                }
            }
            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if(_ptr == IntPtr.Zero) { ThrowNullRef(); }
            CountRef() = 0;
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
        public void Dispose()
        {
            if(_ptr == IntPtr.Zero) { return; }
            ArrayRef().Dispose();
            Marshal.FreeHGlobal(_ptr);
            Unsafe.AsRef(_ptr) = default;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]  // uncommon path
        private void Extend()
        {
            Debug.Assert(_ptr != IntPtr.Zero);
            var count = CountRef();
            if(count == 0) {
                ArrayRef() = new UnsafeRawArray<T>(4, false);
            }
            else {
                ref var array = ref ArrayRef();
                var newArray = new UnsafeRawArray<T>(array.Length * 2, false);
                array.AsSpan().CopyTo(newArray.AsSpan());
                array.Dispose();
                array = newArray;
            }
        }

        private ref int CountRef() => ref *(int*)_ptr;
        private ref UnsafeRawArray<T> ArrayRef() => ref *(UnsafeRawArray<T>*)(((int*)_ptr) + 1);

        public override string? ToString()
        {
            if(_ptr == IntPtr.Zero) { ThrowNullRef(); }
            return base.ToString();
        }

        public override bool Equals(object? obj) => obj is RawList<T> list && Equals(list);

        public bool Equals(RawList<T> other) => _ptr == other._ptr;

        public override int GetHashCode() => _ptr.GetHashCode();

        public static bool operator ==(RawList<T> left, RawList<T> right) => left.Equals(right);

        public static bool operator !=(RawList<T> left, RawList<T> right) => !(left == right);

        [DoesNotReturn]
        private static void ThrowOutOfRange(string message) => throw new ArgumentOutOfRangeException(message);

        [DoesNotReturn]
        private static void ThrowNullRef() => throw new NullReferenceException();
    }

    internal class RawListDebuggerTypeProxy<T> where T : unmanaged
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly RawList<T> _entity;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => _entity.AsSpan().ToArray();

        public RawListDebuggerTypeProxy(RawList<T> entity) => _entity = entity;
    }
}
