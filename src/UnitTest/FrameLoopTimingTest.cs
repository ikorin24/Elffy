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
            // All values of `FrameLoopTiming` can be cast to `ScreenCurrentTiming`. (as a same name)

            var all = Enum.GetNames<FrameLoopTiming>()
                .All(name =>
                {
                    var value = Enum.Parse<FrameLoopTiming>(name);
                    if(value == FrameLoopTiming.None) {
                        return value == (FrameLoopTiming)ScreenCurrentTiming.OutOfFrameLoop;
                    }
                    else {
                        return value == (FrameLoopTiming)Enum.Parse<ScreenCurrentTiming>(name);
                    }
                });
            Assert.True(all);

            Assert.True(Enum.GetValues<FrameLoopTiming>().All(t => t.IsValidOrNone()));
            Assert.True(Enum.GetValues<FrameLoopTiming>().Where(t => t != FrameLoopTiming.None).All(t => t.IsValid()));
        }
    }
}
