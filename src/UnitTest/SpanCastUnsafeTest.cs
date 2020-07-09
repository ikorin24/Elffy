#nullable enable
using Elffy.Effective.Unsafes;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace UnitTest
{
    public class SpanCastUnsafeTest
    {
        [Fact]
        public void CastRefType()
        {
            var objectSpan = new object[100].AsSpan();
            var objSpanSlice = objectSpan.Slice(0, 10);

            var strSpan = SpanCastUnsafe.CastRefType<object, string>(objSpanSlice);
            for(int i = 0; i < strSpan.Length; i++) {
                strSpan[i] = i.ToString();
            }

            for(int i = 0; i < strSpan.Length; i++) {
                Assert.True(objSpanSlice[i] is string);
                Assert.True(strSpan[i] is string);
                Assert.Same(objSpanSlice[i], strSpan[i]);
            }

            objSpanSlice.Clear();

            for(int i = 0; i < strSpan.Length; i++) {
                Assert.Null(objSpanSlice[i]);
                Assert.Null(strSpan[i]);
            }
        }
    }
}
