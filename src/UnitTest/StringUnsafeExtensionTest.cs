#nullable enable
using System;
using Elffy.Core;
using Elffy.Components;
using Xunit;
using Elffy.Effective.Unsafes;
using System.Runtime.CompilerServices;

namespace UnitTest
{
    public class StringUnsafeExtensionTest
    {
        [Fact]
        public void BasicAPI()
        {
            // const string
            Check("This is test");
            // not const string
            Check(12345.ToString());
            // const empty string
            Check("");
            // not const empty string
            var empty = ",".Split(',', StringSplitOptions.None)[0];
            Assert.True(empty == "");
            Check(empty);
        }

        private static void Check(string str)
        {
            ref var firstChar = ref str.GetFirstCharReference();

            if(str.Length > 0) {
                ref var a = ref Unsafe.AsRef(in str.AsSpan()[0]);
                var same = Unsafe.AreSame(ref a, ref firstChar);
                Assert.True(same);
            }
            else {
                // string is null-terminated.
                // First char of empty string is '\0'
                Assert.True(firstChar == '\0');
            }
        }
    }
}
