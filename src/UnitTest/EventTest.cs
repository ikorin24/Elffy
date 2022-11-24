#nullable enable
using System;
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
            Assert.Equal(0, sample.SubscribedCount);
            using(var bag = new SubscriptionBag()) {
                sample.TestEvent.Subscribe(x => flag = true).AddTo(bag);
                Assert.Equal(1, sample.SubscribedCount);
                sample.RaiseTest();
                Assert.True(flag);
            }
            Assert.Equal(0, sample.SubscribedCount);
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
            Assert.Equal(0, sample.SubscribedCount);
            using(var bag = new SubscriptionBag()) {
                for(int i = 0; i < subscribeCount; i++) {
                    var num = i;
                    sample.TestEvent.Subscribe(x =>
                    {
                        called[num] = true;
                        Assert.Equal(num, state);
                        state++;
                    }).AddTo(bag);
                }
                Assert.Equal(subscribeCount, sample.SubscribedCount);
                sample.RaiseTest();
                Assert.True(called.All(x => x));
            }
            Assert.Equal(0, sample.SubscribedCount);
        }

        [Fact]
        public void SubscribeCovariance()
        {
            var sample = new Sample();
            using var subscriptions = new SubscriptionBag();
            Action<object> objectAction1 = (object sender) =>
            {
                Assert.Equal(typeof(Sample), sender.GetType());
            };

            Action<object> objectAction2 = (object sender) =>
            {
                Assert.Equal(typeof(Sample), sender.GetType());
            };

            Action<Sample> sampleAction = (Sample sender) =>
            {
                Assert.Equal(typeof(Sample), sender.GetType());
            };

            sample.TestEvent.Subscribe(objectAction1).AddTo(subscriptions.Register);
            sample.TestEvent.Subscribe(objectAction2).AddTo(subscriptions.Register);
            sample.TestEvent.Subscribe(sampleAction).AddTo(subscriptions.Register);

            sample.RaiseTest();
        }

        private sealed class Sample
        {
            private EventSource<Sample> _testEvent;

            public Event<Sample> TestEvent => _testEvent.Event;

            public int SubscribedCount => _testEvent.SubscribedCount;

            public void RaiseTest()
            {
                _testEvent.Invoke(this);
            }
        }
    }
}
