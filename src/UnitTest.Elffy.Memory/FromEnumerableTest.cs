#nullable enable
using System.Linq;
using Elffy.Effective.Unsafes;
using System;
using Xunit;
using Elffy;
using Elffy.Effective;
using System.Collections.Generic;
using System.Collections;

namespace UnitTest
{
    public class FromEnumerableTest
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(64)]
        [InlineData(100)]
        [InlineData(10000)]
        public void CollectEnumerable(int count)
        {
            var source = Enumerable.Range(0, count);
            Assert.True(source is not int[]);
            Assert.True(source is not ICollection<int>);
            Assert.True(source is not IReadOnlyCollection<int>);
            TestCollect(source);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(64)]
        [InlineData(100)]
        [InlineData(10000)]
        public void CollectArray(int count)
        {
            var source = Enumerable.Range(0, count).ToArray();
            Assert.True(source is int[]);
            TestCollect(source);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(64)]
        [InlineData(100)]
        [InlineData(10000)]
        public void CollectList(int count)
        {
            var source = Enumerable.Range(0, count).ToList();
            Assert.True(source is List<int>);
            TestCollect(source);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(64)]
        [InlineData(100)]
        [InlineData(10000)]
        public void CollectNonEnumeratedCounted(int count)
        {
            var source = new TestCollection<int>(Enumerable.Range(0, count).ToArray());

            // I can get count of source without enumeration.
            Assert.True(source is ICollection<int>);
            TestCollect(source);
        }

        private static void TestCollect(IEnumerable<int> source)
        {
            var answer = source.ToArray();
            var answerString = source.Select(x => x.ToString()).ToArray();

            using(var collection = source.Collect<int, UnsafeRawArray<int>>()) {
                Assert.True(collection.AsSpan().SequenceEqual(answer));
            }
            using(var collection = source.Collect<int, UnsafeRawList<int>>()) {
                Assert.True(collection.AsSpan().SequenceEqual(answer));
            }
            using(var collection = source.Collect<int, PooledArray<int>>()) {
                Assert.True(collection.AsSpan().SequenceEqual(answer));
            }
            using(var collection = source.Collect<int, ValueTypeRentMemory<int>>()) {
                Assert.True(collection.AsSpan().SequenceEqual(answer));
            }
            using(var collection = source.Select(x => x.ToString()).Collect<string, RefTypeRentMemory<string>>()) {
                Assert.True(collection.AsSpan().SequenceEqual(answerString));
            }
        }

        private sealed class TestCollection<T> : ICollection<T>
        {
            private readonly T[] _values;

            public TestCollection(T[] values)
            {
                _values = values;
            }

            public int Count => _values.Length;

            public bool IsReadOnly => true;

            public IEnumerator<T> GetEnumerator() => (_values as IEnumerable<T>).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

            public void Add(T item) => throw new NotSupportedException();

            public void Clear() => throw new NotSupportedException();

            public bool Contains(T item) => _values.Contains(item);

            public void CopyTo(T[] array, int arrayIndex) => _values.CopyTo(array, arrayIndex);

            public bool Remove(T item) => throw new NotSupportedException();
        }
    }
}
