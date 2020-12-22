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
                var list = new List<int>();
                var span = Enumerable.Range(0, 20).ToArray().AsSpan();
                list.AddRange(span);
                for(int i = 0; i < list.Count; i++) {
                    Assert.True(i == list[i]);
                }
            }

            {
                var list = Enumerable.Range(0, 50).ToList();
                var span = Enumerable.Range(50, 20).ToArray().AsSpan();
                list.AddRange(span);
                for(int i = 0; i < list.Count; i++) {
                    Assert.True(i == list[i]);
                }
            }
        }
    }
}
