#nullable enable
using System.Linq;
using Elffy.Effective.Unsafes;
using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace UnitTest
{
    public class UnsafeRawListTest
    {
        [Fact]
        public void New()
        {
            using(var list = UnsafeRawList<int>.Null) {
                Assert.True(list == null);
                Assert.False(list != null);
                Assert.True(null == list);
                Assert.False(null != list);
                Assert.True(list.Equals(list));

                Assert.Throws<NullReferenceException>(() => list.Count);
                Assert.Throws<NullReferenceException>(() => list.Capacity);
                Assert.Throws<NullReferenceException>(() => list.Ptr);
                Assert.Throws<NullReferenceException>(() => list.Add(0));
            }

            using(var list = UnsafeRawList<int>.New()) {
                Assert.Equal(0, list.Count);
                Assert.True(list.Capacity >= 0);
                Assert.NotEqual(IntPtr.Zero, list.Ptr);
            }

            using(var list = UnsafeRawList<int>.New(capacity: 10)) {
                Assert.Equal(0, list.Count);
                Assert.Equal(10, list.Capacity);
                Assert.NotEqual(IntPtr.Zero, list.Ptr);
            }

            ReadOnlySpan<int> source = Enumerable.Range(0, 100).ToArray();
            using(var list = UnsafeRawList<int>.New(source)) {
                Assert.Equal(source.Length, list.Count);
                Assert.True(source.Length >= list.Capacity);
                Assert.NotEqual(IntPtr.Zero, list.Ptr);
                Assert.True(list.AsSpan().SequenceEqual(source));
            }
        }

        [Fact]
        public void Add()
        {
            using(var list = UnsafeRawList<int>.New()) {
                const int Count = 10;
                for(int i = 0; i < Count; i++) {
                    list.Add(i);
                }
                for(int i = 0; i < list.Count; i++) {
                    Assert.Equal(i, list[i]);
                }
            }

            ReadOnlySpan<int> source = Enumerable.Range(0, 100).ToArray();

            using(var list = UnsafeRawList<int>.New(source)) {
                list.AddRange(source);
                Assert.True(list.AsSpan(0, source.Length).SequenceEqual(source));
                Assert.True(list.AsSpan(source.Length, source.Length).SequenceEqual(source));
            }
        }

        [Fact]
        public void Clear()
        {
            ReadOnlySpan<int> source = Enumerable.Range(0, 10).ToArray();

            using(var list = UnsafeRawList<int>.New(source)) {
                list.Clear();
                Assert.Equal(0, list.Count);
                Assert.True(list.Capacity >= 0);
            }
        }

        [Fact]
        public void IndexOf()
        {
            ReadOnlySpan<int> source = Enumerable.Range(0, 10).ToArray();

            using(var list = UnsafeRawList<int>.New(source)) {
                for(int i = 0; i < list.Count; i++) {
                    Assert.True(list.IndexOf(i) == list[i]);
                }
            }
        }

        [Fact]
        public void Remove()
        {
            const int Count = 10;
            ReadOnlySpan<int> source = Enumerable.Range(0, Count).ToArray();
            using(var list = UnsafeRawList<int>.New(source)) {
                for(int i = 0; i < Count; i++) {
                    var removed = list.Remove(i);
                    Assert.True(removed);
                    Assert.Equal(Count - i - 1, list.Count);
                }
            }
        }

        [Fact]
        public void RemoveAt()
        {
            const int Count = 10;
            ReadOnlySpan<int> source = Enumerable.Range(0, Count).ToArray();
            using(var list = UnsafeRawList<int>.New(source)) {
                Assert.True(list.AsSpan().SequenceEqual(source));
                list.RemoveAt(5);
                Assert.True(list.AsSpan().SequenceEqual(new[] { 0, 1, 2, 3, 4, 6, 7, 8, 9 }));
                list.RemoveAt(0);
                Assert.True(list.AsSpan().SequenceEqual(new[] { 1, 2, 3, 4, 6, 7, 8, 9 }));
                list.RemoveAt(1);
                Assert.True(list.AsSpan().SequenceEqual(new[] { 1, 3, 4, 6, 7, 8, 9 }));
                list.RemoveAt(6);
                Assert.True(list.AsSpan().SequenceEqual(new[] { 1, 3, 4, 6, 7, 8, }));
            }
        }

        [Fact]
        public unsafe void GetReference()
        {
            using(var list = UnsafeRawList<int>.New(capacity: 0)) {
                Assert.True(Unsafe.AsPointer(ref list.GetReference()) == null);
            }

            using(var list = UnsafeRawList<int>.New(new[] { 0, 1, 2, 3, 4 })) {
                Assert.True(Unsafe.AreSame(ref list.GetReference(), ref list[0]));
            }
        }
    }
}
