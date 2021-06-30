#nullable enable
using System;
using Xunit;
using Elffy.Effective;
using System.Linq;

namespace UnitTest
{
    public class WeakReferenceArrayTest
    {
        [Fact]
        public void ReleaseHandle()
        {
            var array = new WeakReferenceArray<string>(10);
            try {
                AddItems(array);
                GC.Collect();
                Assert.Null(array[0]);
            }
            finally {
                array.Dispose();
            }
            // Length get zero after disposed
            Assert.Equal(0, array.Length);

            static void AddItems(WeakReferenceArray<string> array)
            {
                var str = new string('0', 3);
                array[0] = str;
                Assert.Equal("000", array[0]);

                // 'str' get unreachable here.
            }
        }

        [Fact]
        public void Enumeration()
        {
            var keep = Enumerable.Range(0, 10).Select(i => i.ToString()).ToArray();
            using var array = new WeakReferenceArray<string>(keep);

            // Enumeration by IEnumerable<T>
            Assert.True(array.SequenceEqual(keep));

            // Enumeration by foreach
            {
                int i = 0;
                foreach(string? item in array) {
                    Assert.Equal(keep[i++], item);
                }
            }

            // Enumeration by non-generic IEnumerable
            {
                int i = 0;
                var nonGeneric = (System.Collections.IEnumerable)array;
                foreach(object? item in nonGeneric) {
                    Assert.True(keep[i++] == item as string);
                }
            }

            // Enumeration by for
            {
                for(int i = 0; i < array.Length; i++) {
                    Assert.Equal(keep[i], array[i]);
                }
            }
        }
    }
}
