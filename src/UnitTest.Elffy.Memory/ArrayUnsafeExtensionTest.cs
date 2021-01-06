#nullable enable
using System.Linq;
using Elffy.Effective;
using Elffy.Effective.Unsafes;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

namespace UnitTest
{
    public class ArrayUnsafeExtensionTest
    {
        [Fact]
        public void At()
        {
            var array = Enumerable.Range(0, 100).ToArray();
            for(int i = 0; i < array.Length; i++) {
                Assert.True(Unsafe.AreSame(ref array[i], ref array.At(i)));
                Assert.True(array[i] == array.At(i));
            }
        }

        [Fact]
        public void GetReference()
        {
            var array = Enumerable.Range(0, 100).ToArray();
            Assert.True(Unsafe.AreSame(ref array[0], ref array.GetReference()));
        }

        [Fact]
        public void AsSpanUnsafe()
        {
            var array = Enumerable.Range(0, 100).ToArray();
            Assert.True(array.AsSpanUnsafe().SequenceEqual(array.AsSpan()));
            Assert.True(array.AsSpanUnsafe(0).SequenceEqual(array.AsSpan(0)));
            Assert.True(array.AsSpanUnsafe(10).SequenceEqual(array.AsSpan(10)));
            Assert.True(array.AsSpanUnsafe(20, 40).SequenceEqual(array.AsSpan(20, 40)));

            Assert.Throws<NullReferenceException>(() =>
            {
                (null as int[])!.AsSpanUnsafe();    // The method thorws null reference exception.
            });
        }
    }
}
