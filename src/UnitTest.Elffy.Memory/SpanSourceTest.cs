#nullable enable
using System.Linq;
using Elffy.Effective;
using System;
using Xunit;
using System.Diagnostics;

namespace UnitTest
{
    public class SpanSourceTest
    {
        [Theory]
        [InlineData(0, null, null)]
        [InlineData(1, null, null)]
        [InlineData(10, null, null)]
        [InlineData(100, 37, null)]
        [InlineData(100, 53, 19)]
        [InlineData(100, 86, 14)]
        public void SpanSourceFromArray(int n, int? start, int? length)
        {
            var array = Enumerable.Range(0, n).ToArray();
            var ok = (start, length) switch
            {
                (null, null) => array.AsSpanSource().AsSpan().SequenceEqual(array.AsSpan()),
                (int s, null) => array.AsSpanSource(s).AsSpan().SequenceEqual(array.AsSpan(s)),
                (int s, int l) => array.AsSpanSource(s, l).AsSpan().SequenceEqual(array.AsSpan(s, l)),
                _ => throw new UnreachableException(),
            };
            Assert.True(ok);
        }

        [Theory]
        [InlineData(0, null, null)]
        [InlineData(1, null, null)]
        [InlineData(10, null, null)]
        [InlineData(100, 37, null)]
        [InlineData(100, 53, 19)]
        [InlineData(100, 86, 14)]
        public void ReadOnlySpanSourceFromArray(int n, int? start, int? length)
        {
            var array = Enumerable.Range(0, n).ToArray();
            var ok = (start, length) switch
            {
                (null, null) => array.AsReadOnlySpanSource().AsReadOnlySpan().SequenceEqual(array.AsSpan()),
                (int s, null) => array.AsReadOnlySpanSource(s).AsReadOnlySpan().SequenceEqual(array.AsSpan(s)),
                (int s, int l) => array.AsReadOnlySpanSource(s, l).AsReadOnlySpan().SequenceEqual(array.AsSpan(s, l)),
                _ => throw new UnreachableException(),
            };
            Assert.True(ok);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public void SpanSourceFromList(int n)
        {
            var list = Enumerable.Range(0, n).ToList();
            var spanSource = list.AsSpanSource();
            Assert.True(spanSource.AsSpan().SequenceEqual(list.AsSpan()));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public void ReadOnlySpanSourceFromList(int n)
        {
            var list = Enumerable.Range(0, n).ToList();
            var spanSource = list.AsReadOnlySpanSource();
            Assert.True(spanSource.AsReadOnlySpan().SequenceEqual(list.AsSpan()));
        }

        [Theory]
        [InlineData(-1, null)]
        [InlineData(-1, 5)]
        [InlineData(4, 7)]
        public void ThrowTest(int start, int? length)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                (start, length) switch
                {
                    (int s, null) => () => Enumerable.Range(0, 10).ToArray().AsSpanSource(s),
                    (int s, int l) => () => Enumerable.Range(0, 10).ToArray().AsSpanSource(s, l),
                });
            Assert.Throws<ArgumentOutOfRangeException>(
                (start, length) switch
                {
                    (int s, null) => () => Enumerable.Range(0, 10).ToArray().AsReadOnlySpanSource(s),
                    (int s, int l) => () => Enumerable.Range(0, 10).ToArray().AsReadOnlySpanSource(s, l),
                });
        }
    }
}
