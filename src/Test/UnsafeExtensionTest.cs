#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Elffy.Effective.Internal;
using System.Collections.ObjectModel;
using System;
using System.Collections.Generic;

namespace Test
{
    [TestClass]
    public class UnsafeExtensionTest
    {
        [TestMethod]
        public void ListAsReadOnlyMemoryTest()
        {
            var list = Enumerable.Range(0, 10).ToList();
            var i = 0;
            foreach(var item in list.AsReadOnlyMemory().Span) {
                Assert.IsTrue(i == item);
                i++;
            }
        }

        [TestMethod]
        public void ListAddRageTest()
        {
            {
                var list = new List<int>();
                var span = Enumerable.Range(0, 20).ToArray().AsSpan();
                list.AddRange(span);
                for(int i = 0; i < list.Count; i++) {
                    Assert.IsTrue(i == list[i]);
                }
            }

            {
                var list = Enumerable.Range(0, 50).ToList();
                var span = Enumerable.Range(50, 20).ToArray().AsSpan();
                list.AddRange(span);
                for(int i = 0; i < list.Count; i++) {
                    Assert.IsTrue(i == list[i]);
                }
            }
        }

        [TestMethod]
        public void ReadOnlyCollectionToSpanTest()
        {
            {
                var readOnlyCollection = Enumerable.Range(0, 10).ToList().AsReadOnly();
                var i = 0;
                foreach(var item in readOnlyCollection.AsSpan()) {
                    Assert.IsTrue(i == item);
                    i++;
                }
            }


            {
                var readOnlyCollection2 = new ReadOnlyCollection<int>(Enumerable.Range(0, 10).ToArray());
                var i = 0;
                foreach(var item in readOnlyCollection2.AsSpan()) {
                    Assert.IsTrue(i == item);
                    i++;
                }
            }
        }
    }
}
