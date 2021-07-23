#nullable enable
using System;
using System.Linq;
using Xunit;
using Elffy;

namespace UnitTest
{
    public class FrameLoopTimingTest
    {
        [Fact]
        public void CastTest()
        {
            var allTimings = Enum.GetValues<FrameLoopTiming>();
            var specifiedTimings = allTimings
                                      .Where(t => t != FrameLoopTiming.NotSpecified)
                                      .ToArray();

            // All values of `FrameLoopTiming` can be cast to `ScreenCurrentTiming` except NotSpecified. (as a same name)

            var isSameName = specifiedTimings
                .Select(t => t.ToString())
                .All(name => Enum.Parse<FrameLoopTiming>(name) == (FrameLoopTiming)Enum.Parse<ScreenCurrentTiming>(name));
            Assert.True(isSameName);
            Assert.True((byte)FrameLoopTiming.NotSpecified == (byte)ScreenCurrentTiming.OutOfFrameLoop);
            Assert.True(sizeof(FrameLoopTiming) == sizeof(byte));
            Assert.True(sizeof(ScreenCurrentTiming) == sizeof(byte));


            Assert.True(specifiedTimings.All(t => t.IsSpecified()));
            Assert.True(FrameLoopTiming.NotSpecified.IsSpecified() == false);
            Assert.True(allTimings.All(t => t.IsValid()));
        }
    }
}
