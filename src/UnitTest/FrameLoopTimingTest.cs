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
                          .All(name => Enum.Parse<FrameLoopTiming>(name) == (FrameLoopTiming)Enum.Parse<ScreenCurrentTiming>(name));
            Assert.True(all);

            Assert.True(Enum.GetValues<FrameLoopTiming>().All(v => v.IsValid()));
        }
    }
}
