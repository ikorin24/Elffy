#nullable enable
using Elffy.Effective;
using Elffy.Effective.Unsafes;
using System;
using System.Buffers;
using System.Collections.Generic;

namespace Elffy
{
    public interface IFromEnumerable<TSelf, TItem> :
        IConstruct<TSelf, IEnumerable<TItem>>
        where TSelf : IFromEnumerable<TSelf, TItem>
    {
        abstract static TSelf From(IEnumerable<TItem> source);

        static TSelf IConstruct<TSelf, IEnumerable<TItem>>.New(in IEnumerable<TItem> arg) => TSelf.From(arg);
    }

    public static class FromEnumerableExtensions
    {
        public static TCollection Collect<T, TCollection>(this IEnumerable<T> source)
            where TCollection : IFromEnumerable<TCollection, T>
        {
            return TCollection.From(source);
        }
    }

    internal static class FromEnumerableImplHelper
    {
        public static TCollection EnumerateCollectBlittable<T, TCollection>(IEnumerable<T> source, ReadOnlySpanFunc<T, TCollection> collector)
            where T : unmanaged
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(collector);

            const int InitialCapacity = 64;
            var bufMem = new ValueTypeRentMemory<T>(InitialCapacity, false, out var buf);
            try {
                int i = 0;
                foreach(var item in source) {
                    if(i == buf.Length) {
                        if(buf.Length == int.MaxValue) {
                            throw new OverflowException();
                        }
                        var newBufLen = (buf.Length < 0x40000000) ? buf.Length * 2 : int.MaxValue;
                        var newBufMem = new ValueTypeRentMemory<T>(newBufLen, false, out var newBuf);
                        buf.CopyTo(newBuf);
                        bufMem.Dispose();
                        bufMem = newBufMem;
                        buf = newBuf;
                    }
                    buf.At(i++) = item;
                }
                return collector.Invoke(buf.Slice(0, i));
            }
            finally {
                bufMem.Dispose();
            }
        }

        public static TCollection EnumerateCollectRef<T, TCollection>(IEnumerable<T> source, ReadOnlySpanFunc<T, TCollection> collector)
            where T : class?
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(collector);
            const int InitialCapacity = 64;
            var bufMem = new RefTypeRentMemory<T>(InitialCapacity, out var buf);
            try {
                int i = 0;
                foreach(var item in source) {
                    if(i == buf.Length) {
                        if(buf.Length == int.MaxValue) {
                            throw new OverflowException();
                        }
                        var newBufLen = (buf.Length < 0x40000000) ? buf.Length * 2 : int.MaxValue;
                        var newBufMem = new RefTypeRentMemory<T>(newBufLen, out var newBuf);
                        buf.CopyTo(newBuf);
                        bufMem.Dispose();
                        bufMem = newBufMem;
                        buf = newBuf;
                    }
                    buf.At(i++) = item;
                }
                return collector.Invoke(buf.Slice(0, i));
            }
            finally {
                bufMem.Dispose();
            }
        }

        public static T[] EnumerateCollectToPooledArray<T>(IEnumerable<T> source, out int usedLength)
        {
            ArgumentNullException.ThrowIfNull(source);

            const int InitialCapacity = 64;
            var buf = ArrayPool<T>.Shared.Rent(InitialCapacity);
            int i = 0;
            foreach(var item in source) {
                if(i == buf.Length) {
                    if(buf.Length == int.MaxValue) {
                        throw new OverflowException();
                    }
                    var newBufLenRequested = (buf.Length < 0x40000000) ? buf.Length * 2 : int.MaxValue;
                    var newBuf = ArrayPool<T>.Shared.Rent(newBufLenRequested);
                    buf.AsSpan().CopyTo(newBuf);
                    ArrayPool<T>.Shared.Return(buf);
                    buf = newBuf;
                }
                buf.At(i++) = item;
            }
            usedLength = i;
            return buf;
        }
    }
}
