#nullable enable
using System.Linq;
using Elffy.Effective.Unsafes;
using System;
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
    }
}
