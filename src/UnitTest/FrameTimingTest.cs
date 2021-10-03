#nullable enable
using System;
using System.Linq;
using Xunit;
using Elffy;

namespace UnitTest
{
    public class FrameTimingTest
    {
        [Fact]
        public void CastTest()
        {
            var allTimings = Enum.GetValues<FrameTiming>();
            var specifiedTimings = allTimings
                                      .Where(t => t != FrameTiming.NotSpecified)
                                      .ToArray();

            // All values of `FrameTiming` can be cast to `ScreenCurrentTiming` except NotSpecified. (as a same name)

            var isSameName = specifiedTimings
                .Select(t => t.ToString())
                .All(name => Enum.Parse<FrameTiming>(name) == (FrameTiming)Enum.Parse<ScreenCurrentTiming>(name));
            Assert.True(isSameName);
            Assert.True((byte)FrameTiming.NotSpecified == (byte)ScreenCurrentTiming.OutOfFrameLoop);
            Assert.True(sizeof(FrameTiming) == sizeof(byte));
            Assert.True(sizeof(ScreenCurrentTiming) == sizeof(byte));


            Assert.True(specifiedTimings.All(t => t.IsSpecified()));
            Assert.True(FrameTiming.NotSpecified.IsSpecified() == false);
            Assert.True(allTimings.All(t => t.IsValid()));
        }
    }
}
