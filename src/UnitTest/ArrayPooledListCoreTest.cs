#nullable enable
using System.Runtime.CompilerServices;
using Elffy.Mathematics;
using Elffy.Features.Internal;
using Xunit;

namespace UnitTest
{
    public class ArrayPooledListCoreTest
    {
        [Fact]
        public void InstanceCachingTest()
        {
            // This is only for test code.
            // 'listCore' is actually on the heap memory.
            var listCore = new ArrayPooledListCore<string>();

            Assert.True(listCore.Count == 0);
            Assert.True(listCore.Capacity == 0);

            listCore.Add(0.ToString());
            var innerArray = Unsafe.As<ArrayPooledListCore<string>, Dummy<string>>(ref listCore).Array;

            for(int i = 1; i < 10; i++) {
                listCore.Add(i.ToString());
                Assert.True(listCore.Count == i + 1);
                Assert.True(MathTool.IsPowerOfTwo(listCore.Capacity));
            }

            // Create new listCore
            var listCore2 = new ArrayPooledListCore<string>();
            listCore2.Add("a");
            var innerArray2 = Unsafe.As<ArrayPooledListCore<string>, Dummy<string>>(ref listCore2).Array;

            // Inner array is cached in the pool.
            Assert.True(innerArray == innerArray2);
        }

        /// <summary>Dummy object only for unit test, whose memory layout must be same as <see cref="ArrayPooledListCore{T}"/></summary>
        /// <typeparam name="T"></typeparam>
        private struct Dummy<T>
        {
#pragma warning disable 0649
            public T[]? Array;
            public int Count;
#pragma warning restore 0649
        }
    }
}
