#nullable enable
using Cysharp.Threading.Tasks;
using Elffy;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace UnitTest
{
    public class AsyncEventTest
    {
        [Fact]
        public async Task SyncSubscribe()
        {
            var value = 0;
            var foo = new Foo();
            Assert.Equal(0, foo.SubscibedCount);
            var unsbscriber = foo.Test.Subscribe((sender, ct) =>
            {
                value++;
                return UniTask.CompletedTask;
            });
            Assert.True(value == 0);
            Assert.Equal(1, foo.SubscibedCount);
            await foo.SequentiallyRaiseTest(CancellationToken.None);
            Assert.True(value == 1);
            Assert.Equal(1, foo.SubscibedCount);
            unsbscriber.Dispose();
            Assert.Equal(0, foo.SubscibedCount);
            await foo.SequentiallyRaiseTest(CancellationToken.None);
            Assert.True(value == 1);
            Assert.Equal(0, foo.SubscibedCount);
        }

        [Fact]
        public async Task AsyncSubscribe()
        {
            var value = 0;
            var foo = new Foo();
            Assert.Equal(0, foo.SubscibedCount);
            var unsbscriber = foo.Test.Subscribe(async (sender, ct) =>
            {
                value += 5;
                await Task.Delay(10, ct);
                value++;
            });
            Assert.True(value == 0);
            Assert.Equal(1, foo.SubscibedCount);
            await foo.SequentiallyRaiseTest(CancellationToken.None);
            Assert.True(value == 6);
            Assert.Equal(1, foo.SubscibedCount);
            unsbscriber.Dispose();
            Assert.Equal(0, foo.SubscibedCount);
            await foo.SequentiallyRaiseTest(CancellationToken.None);
            Assert.True(value == 6);
            Assert.Equal(0, foo.SubscibedCount);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(20)]
        public async Task MultiSubscribe(int delegateCount)
        {
            var array = new int[delegateCount];
            var unsubscribers = new AsyncEventUnsubscriber<Foo>[delegateCount];
            var foo = new Foo();
            Assert.Equal(0, foo.SubscibedCount);

            for(int i = 0; i < delegateCount; i++) {
                var num = i;
                if(i % 2 == 0) {
                    unsubscribers[i] = foo.Test.Subscribe(async (sender, ct) =>
                    {
                        array[num] += 5;
                        await Task.Delay(10, ct);
                        array[num]++;
                    });
                }
                else {
                    unsubscribers[i] = foo.Test.Subscribe((sender, ct) =>
                    {
                        array[num] += 5;
                        array[num]++;
                        return UniTask.CompletedTask;
                    });
                }
            }
            Assert.True(array.All(x => x == 0));
            Assert.Equal(delegateCount, foo.SubscibedCount);
            await foo.SequentiallyRaiseTest(CancellationToken.None);
            Assert.True(array.All(x => x == 6));
            Assert.Equal(delegateCount, foo.SubscibedCount);
            foreach(var u in unsubscribers) {
                u.Dispose();
            }
            await foo.SequentiallyRaiseTest(CancellationToken.None);
            Assert.True(array.All(x => x == 6));
            Assert.Equal(0, foo.SubscibedCount);
        }

        [Fact]
        public async Task AlreadyCanceled_SyncDelegate()
        {
            var foo = new Foo();
            Assert.Equal(0, foo.SubscibedCount);
            var unsbscriber = foo.Test.Subscribe((sender, ct) =>
            {
                // No one should not come here.
                Assert.True(false, "No one should not come here.");
                return UniTask.CompletedTask;
            });
            Assert.Equal(1, foo.SubscibedCount);
            var raisedEventTask = foo.SequentiallyRaiseTest(new CancellationToken(true));
            Assert.Equal(UniTaskStatus.Canceled, raisedEventTask.Status);
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await raisedEventTask);
            Assert.Equal(1, foo.SubscibedCount);
            unsbscriber.Dispose();
            Assert.Equal(0, foo.SubscibedCount);
        }

        [Fact]
        public async Task AlreadyCanceled_NoDelegates()
        {
            // An OperationCanceledException is thrown even if no one subscribes the event.

            var foo = new Foo();
            Assert.Equal(0, foo.SubscibedCount);
            var nonSubscribedEventTask = foo.SequentiallyRaiseTest(new CancellationToken(true));
            Assert.Equal(UniTaskStatus.Canceled, nonSubscribedEventTask.Status);
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await nonSubscribedEventTask);
            Assert.Equal(0, foo.SubscibedCount);
        }

        [Fact]
        public async Task AlreadyCanceled_AsyncDelegate()
        {
            var foo = new Foo();
            Assert.Equal(0, foo.SubscibedCount);
            var unsbscriber = foo.Test.Subscribe(async (sender, ct) =>
            {
                // No one should not come here.
                Assert.True(false, "No one should not come here.");
                await UniTask.CompletedTask;
            });
            Assert.Equal(1, foo.SubscibedCount);

            var raiseEventTask = foo.SequentiallyRaiseTest(new CancellationToken(true));
            Assert.Equal(UniTaskStatus.Canceled, raiseEventTask.Status);

            await Assert.ThrowsAsync<OperationCanceledException>(async () => await raiseEventTask);
            Assert.Equal(1, foo.SubscibedCount);
            unsbscriber.Dispose();
            Assert.Equal(0, foo.SubscibedCount);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(20)]
        public async Task AlreadyCanceled_MultiSubscribed(int delegateCount)
        {
            var unsubscribers = new AsyncEventUnsubscriber<Foo>[delegateCount];
            var foo = new Foo();
            Assert.Equal(0, foo.SubscibedCount);

            for(int i = 0; i < delegateCount; i++) {
                var num = i;
                if(i % 2 == 0) {
                    unsubscribers[num] = foo.Test.Subscribe(async (sender, ct) =>
                    {
                        // No one should not come here.
                        Assert.True(false, "No one should not come here.");
                        await UniTask.CompletedTask;
                    });
                }
                else {
                    unsubscribers[num] = foo.Test.Subscribe((sender, ct) =>
                    {
                        // No one should not come here.
                        Assert.True(false, "No one should not come here.");
                        return UniTask.CompletedTask;
                    });
                }
            }
            Assert.Equal(delegateCount, foo.SubscibedCount);
            var raisedEventTask = foo.SequentiallyRaiseTest(new CancellationToken(true));
            Assert.Equal(UniTaskStatus.Canceled, raisedEventTask.Status);
            await Assert.ThrowsAsync<OperationCanceledException>(async () => await raisedEventTask);
            Assert.Equal(delegateCount, foo.SubscibedCount);
            foreach(var u in unsubscribers) {
                u.Dispose();
            }
            Assert.Equal(0, foo.SubscibedCount);
        }

        private sealed class Foo
        {
            private AsyncEventRaiser<Foo>? _test;

            public AsyncEvent<Foo> Test => new AsyncEvent<Foo>(ref _test);

            public int SubscibedCount => _test?.SubscibedCount ?? 0;

            public UniTask SequentiallyRaiseTest(CancellationToken ct)
            {
                return _test.RaiseSequentiallyIfNotNull(this, ct);
            }
        }
    }
}
