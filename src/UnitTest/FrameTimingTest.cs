#nullable enable
using System.Linq;
using Xunit;
using Elffy;

namespace UnitTest
{
    public class FrameTimingTest
    {
        [Fact]
        public unsafe void CastTest()
        {
            {
                var sameTimingPairs = new (FrameTiming FrameTiming, ScreenCurrentTiming CurrentTiming)[]
                {
                    (FrameTiming.NotSpecified, ScreenCurrentTiming.OutOfFrameLoop),
                    (FrameTiming.EarlyUpdate, ScreenCurrentTiming.EarlyUpdate),
                    (FrameTiming.Update, ScreenCurrentTiming.Update),
                    (FrameTiming.LateUpdate, ScreenCurrentTiming.LateUpdate),
                    (FrameTiming.BeforeRendering, ScreenCurrentTiming.BeforeRendering),
                    (FrameTiming.AfterRendering, ScreenCurrentTiming.AfterRendering),
                };
                Assert.True(sameTimingPairs.All(pair => pair.FrameTiming == pair.CurrentTiming));
            }

            {
                var sameNamePairs = new (FrameTiming FrameTiming, ScreenCurrentTiming CurrentTiming)[]
                {
                    (FrameTiming.EarlyUpdate, ScreenCurrentTiming.EarlyUpdate),
                    (FrameTiming.Update, ScreenCurrentTiming.Update),
                    (FrameTiming.LateUpdate, ScreenCurrentTiming.LateUpdate),
                    (FrameTiming.BeforeRendering, ScreenCurrentTiming.BeforeRendering),
                    (FrameTiming.AfterRendering, ScreenCurrentTiming.AfterRendering),
                };
                Assert.True(sameNamePairs.All(pair => pair.FrameTiming.ToString() == pair.CurrentTiming.ToString()));
            }

            {
                var allTimings = FrameTiming.AllValuesEnumerable();
                var specifiedTimings = allTimings.Where(t => t != FrameTiming.NotSpecified).ToArray();
                Assert.True(specifiedTimings.All(t => t.IsSpecified()));
                Assert.True(FrameTiming.NotSpecified.IsSpecified() == false);
                Assert.True(allTimings.All(t => t.IsValid()));
            }
        }
    }
}
