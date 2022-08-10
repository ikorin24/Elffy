#nullable enable
using System.Linq;
using Elffy;
using Xunit;

namespace UnitTest
{
    public class EventTest
    {
        [Fact]
        public void RaiseTest()
        {
            var sample = new Sample();
            var flag = false;
            Assert.Equal(0, sample.SubscibedCount);
            using(var bag = new UnsubscriberBag()) {
                sample.TestEvent.Subscribe(x => flag = true).AddTo(bag);
                Assert.Equal(1, sample.SubscibedCount);
                sample.RaiseTest();
                Assert.True(flag);
            }
            Assert.Equal(0, sample.SubscibedCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(20)]
        public void MultiSubscribe(int subscribeCount)
        {
            var sample = new Sample();
            var state = 0;
            var called = new bool[subscribeCount];
            Assert.Equal(0, sample.SubscibedCount);
            using(var bag = new UnsubscriberBag()) {
                for(int i = 0; i < subscribeCount; i++) {
                    var num = i;
                    sample.TestEvent.Subscribe(x =>
                    {
                        called[num] = true;
                        Assert.Equal(num, state);
                        state++;
                    }).AddTo(bag);
                }
                Assert.Equal(subscribeCount, sample.SubscibedCount);
                sample.RaiseTest();
                Assert.True(called.All(x => x));
            }
            Assert.Equal(0, sample.SubscibedCount);
        }

        private sealed class Sample
        {
            private EventSource<Sample>? _testEvent;

            public Event<Sample> TestEvent => new(ref _testEvent);

            public int SubscibedCount => _testEvent?.SubscibedCount ?? 0;

            public void RaiseTest()
            {
                _testEvent?.Invoke(this);
            }
        }
    }
}
