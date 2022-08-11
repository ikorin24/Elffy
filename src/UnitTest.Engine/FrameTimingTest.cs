#nullable enable
using System.Linq;
using Xunit;
using Elffy;
using System;
using Cysharp.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace UnitTest
{
    [Collection(TestEngineEntryPoint.UseEngineSymbol)]
    public class FrameTimingTest
    {
        [Fact]
        public void StructSizeTest()
        {
            Assert.Equal(sizeof(byte), Unsafe.SizeOf<CurrentFrameTiming>());
            Assert.Equal(sizeof(byte), Unsafe.SizeOf<FrameTiming>());
            Assert.True(Unsafe.SizeOf<CurrentFrameTiming>() == Unsafe.SizeOf<FrameTiming>());
        }

        [Fact]
        public void OrderTest()
        {
            // [NOTE] NotSpecified is not inclueded.
            var timingOrder = new FrameTiming[]
            {
                FrameTiming.FrameInitializing,
                FrameTiming.EarlyUpdate,
                FrameTiming.Update,
                FrameTiming.LateUpdate,
                FrameTiming.BeforeRendering,
                FrameTiming.AfterRendering,
                FrameTiming.FrameFinalizing,
                FrameTiming.Internal_EndOfFrame,
            };
            for(int i = 0; i < timingOrder.Length; i++) {
                for(int j = i; j < timingOrder.Length; j++) {
                    Assert.True(timingOrder[i] <= timingOrder[j]);
                    Assert.True(timingOrder[j] >= timingOrder[i]);
                }
            }

            for(int i = 0; i < timingOrder.Length; i++) {
                for(int j = i + 1; j < timingOrder.Length; j++) {
                    Assert.True(timingOrder[i] < timingOrder[j]);
                    Assert.True(timingOrder[j] > timingOrder[i]);
                    Assert.True(timingOrder[i] <= timingOrder[j]);
                    Assert.True(timingOrder[j] >= timingOrder[i]);
                }
            }


            // NotSpecified always returns false for any large/small comparison
            // except both of them are NotSpecified.
            foreach(var timing in timingOrder) {
                Assert.False(FrameTiming.NotSpecified < timing);
                Assert.False(FrameTiming.NotSpecified > timing);
                Assert.False(FrameTiming.NotSpecified <= timing);
                Assert.False(FrameTiming.NotSpecified >= timing);

                Assert.True(FrameTiming.NotSpecified >= FrameTiming.NotSpecified);
                Assert.True(FrameTiming.NotSpecified <= FrameTiming.NotSpecified);
            }
        }

        [Fact]
        public void CastTest()
        {
            {
                var sameTimingPairs = new (FrameTiming FrameTiming, CurrentFrameTiming CurrentTiming)[]
                {
                    (FrameTiming.FrameInitializing, CurrentFrameTiming.FrameInitializing),
                    (FrameTiming.EarlyUpdate, CurrentFrameTiming.EarlyUpdate),
                    (FrameTiming.Update, CurrentFrameTiming.Update),
                    (FrameTiming.LateUpdate, CurrentFrameTiming.LateUpdate),
                    (FrameTiming.BeforeRendering, CurrentFrameTiming.BeforeRendering),
                    (FrameTiming.AfterRendering, CurrentFrameTiming.AfterRendering),
                    (FrameTiming.FrameFinalizing, CurrentFrameTiming.FrameFinalizing),
                };
                Assert.True(sameTimingPairs.All(pair => pair.FrameTiming == pair.CurrentTiming));
            }

            {
                var sameNamePairs = new (FrameTiming FrameTiming, CurrentFrameTiming CurrentTiming)[]
                {
                    (FrameTiming.FrameInitializing, CurrentFrameTiming.FrameInitializing),
                    (FrameTiming.EarlyUpdate, CurrentFrameTiming.EarlyUpdate),
                    (FrameTiming.Update, CurrentFrameTiming.Update),
                    (FrameTiming.LateUpdate, CurrentFrameTiming.LateUpdate),
                    (FrameTiming.BeforeRendering, CurrentFrameTiming.BeforeRendering),
                    (FrameTiming.AfterRendering, CurrentFrameTiming.AfterRendering),
                    (FrameTiming.FrameFinalizing, CurrentFrameTiming.FrameFinalizing),
                };
                Assert.True(sameNamePairs.All(pair => pair.FrameTiming.ToString() == pair.CurrentTiming.ToString()));
            }

            {
                var allTimings = FrameTiming.AllValues().ToArray();
                var specifiedTimings = allTimings.Where(t => t != FrameTiming.NotSpecified).ToArray();
                Assert.True(specifiedTimings.All(t => t.IsSpecified()));
                Assert.True(FrameTiming.NotSpecified.IsSpecified() == false);

                Assert.True(FrameTiming.AllValues().Span.Contains(default));
                Assert.True(CurrentFrameTiming.AllValues().Span.Contains(default));
            }
        }

        [Fact]
        public void GetTimingPointTest() => TestEngineEntryPoint.Start(screen =>
        {
            var nameValues = FrameTiming.AllNameValues().Span.ToArray();
            foreach(var (name, timing) in nameValues) {
                if(screen.Timings.TryGetTiming(timing, out var timingPoint)) {
                    Assert.NotNull(timingPoint);
                    Assert.Equal(timing, timingPoint.TargetTiming);
                }
                else {
                    Assert.Equal(FrameTiming.NotSpecified, timing);
                }
            }

            foreach(var (name, timing) in nameValues.Where(x => x.Value != FrameTiming.NotSpecified)) {
                var timingPoint = screen.Timings.GetTiming(timing);
                Assert.NotNull(timingPoint);
                Assert.Equal(timing, timingPoint.TargetTiming);
            }

            Assert.Throws<ArgumentException>(() =>
            {
                screen.Timings.GetTiming(FrameTiming.NotSpecified);
            });
        });

        [Fact]
        public void AwaitTimingTest() => TestEngineEntryPoint.Start(async screen =>
        {
            var timingPoints = FrameTiming.AllValues()
                .ToArray()
                .Where(timing => timing != FrameTiming.NotSpecified)
                .Select(timing => screen.Timings.GetTiming(timing))
                .ToArray();

            foreach(var tp in timingPoints) {
                await tp.NextFrame();
                var currentTiming = screen.CurrentTiming;
                Assert.True(currentTiming == tp.TargetTiming, $"expected: {tp.TargetTiming}, actual: {currentTiming}, ");
            }
        });
    }
}
