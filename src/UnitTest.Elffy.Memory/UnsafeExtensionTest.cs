#nullable enable
using System.Linq;
using Elffy.Effective;
using System;
using System.Collections.Generic;
using Xunit;

namespace UnitTest
{
    public class UnsafeExtensionTest
    {
        [Fact]
        public void ListAddRageTest()
        {
            {
                // Empty list
                var list = new List<int>();
                var span = Enumerable.Range(0, 20).ToArray().AsSpan();
                list.AddRange(span);
                for(int i = 0; i < list.Count; i++) {
                    Assert.True(i == list[i]);
                }
            }

            {
                // Empty capacity list
                var list = new List<int>(0);
                var span = Enumerable.Range(0, 20).ToArray().AsSpan();
                list.AddRange(span);
                for(int i = 0; i < list.Count; i++) {
                    Assert.True(i == list[i]);
                }
            }

            {
                // list with values
                var list = Enumerable.Range(0, 50).ToList();
                var span = Enumerable.Range(50, 20).ToArray().AsSpan();
                list.AddRange(span);
                for(int i = 0; i < list.Count; i++) {
                    Assert.True(i == list[i]);
                }
            }

            {
                // Add to self
                var list = Enumerable.Range(0, 50).ToList();
                var span = list.AsSpan();
                list.AddRange(span);
                for(int i = 0; i < 50; i++) {
                    Assert.True(list[i] == list[i + 50]);
                    Assert.True(list[i] == i);
                }
            }
        }
    }
}
