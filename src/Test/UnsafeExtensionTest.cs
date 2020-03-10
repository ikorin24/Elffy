#nullable enable
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Elffy.Effective.Internal;
using System.Collections.ObjectModel;

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
        public void ReadOnlyCollectionToSpanTest()
        {
            {
                var readOnlyCollection = Enumerable.Range(0, 10).ToList().AsReadOnly();
                var i = 0;
                foreach(var item in readOnlyCollection.ExtractInnerArray()) {
                    Assert.IsTrue(i == item);
                    i++;
                }
            }


            {
                var readOnlyCollection2 = new ReadOnlyCollection<int>(Enumerable.Range(0, 10).ToArray());
                var i = 0;
                foreach(var item in readOnlyCollection2.ExtractInnerArray()) {
                    Assert.IsTrue(i == item);
                    i++;
                }
            }
        }
    }
}
