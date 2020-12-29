#nullable enable
using System;
using Xunit;
using System.Linq;
using UnmanageUtility;
using Elffy.Effective;

namespace UnitTest
{
    public class UnmanagedMemoryTest
    {
        [Fact]
        public void UnmanagedMemory()
        {
            using(var umArray = Enumerable.Range(0, 100).ToUnmanagedArray()) {
                var memory = umArray.AsUnmanagedMemory();

                Assert.True(umArray.AsSpan().SequenceEqual(memory.Span));
                Assert.True(umArray.AsSpan().Slice(40).SequenceEqual(memory.Slice(40).Span));
                Assert.True(umArray.AsSpan().Slice(40, 10).SequenceEqual(memory.Slice(40, 10).Span));
                Assert.True(umArray.AsSpan().Slice(99).SequenceEqual(memory.Slice(99).Span));
                Assert.True(umArray.AsSpan().Slice(0).SequenceEqual(memory.Slice(0).Span));
                Assert.True(umArray.AsSpan().Slice(99, 0).SequenceEqual(memory.Slice(99, 0).Span));
            }
        }
    }
}
