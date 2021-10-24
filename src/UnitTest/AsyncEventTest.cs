#nullable enable
using Cysharp.Threading.Tasks;
using Elffy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace UnitTest
{
    public class AsyncEventTest
    {
        private static readonly Func<TestSample, CancellationToken, UniTask> Sync_IncrementValue = (sender, ct) =>
        {
            sender.Value++;
            return UniTask.CompletedTask;
        };

        private static readonly Func<TestSample, CancellationToken, UniTask> Sync_ShouldNotBeCalled = (sender, ct) =>
        {
            // No one should not come here.
            Assert.True(false, "No one should not come here.");
            return UniTask.CompletedTask;
        };

        [Fact]
        public async Task SyncSubscribe()
        {
            var condition = new TestCondition()
            {
                Delegates = new[] { Sync_IncrementValue, },
                CancellationToken = CancellationToken.None,
                TaskStatus = UniTaskStatus.Succeeded,
                Assertion = target => Assert.Equal(1, target.Value),
            };
            await ExecuteTest(condition);
        }

        [Fact]
        public async Task AsyncSubscribe()
        {
            var value = 0;
            var foo = new TestSample();
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
            var unsubscribers = new AsyncEventUnsubscriber<TestSample>[delegateCount];
            var foo = new TestSample();
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
            var condition = new TestCondition()
            {
                Delegates = new[] { Sync_ShouldNotBeCalled, },
                CancellationToken = new CancellationToken(true),
                TaskStatus = UniTaskStatus.Canceled,
                Assertion = target => Assert.Equal(0, target.Value),
            };
            await ExecuteTest(condition);
        }

        [Fact]
        public async Task AlreadyCanceled_NoDelegates()
        {
            // An OperationCanceledException is thrown even if no one subscribes the event.

            var condition = new TestCondition()
            {
                CancellationToken = new CancellationToken(true),
                TaskStatus = UniTaskStatus.Canceled,
                Assertion = target => Assert.Equal(0, target.Value),
            };
            await ExecuteTest(condition);
        }

        [Fact]
        public async Task AlreadyCanceled_AsyncDelegate()
        {
            var foo = new TestSample();
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
            var unsubscribers = new AsyncEventUnsubscriber<TestSample>[delegateCount];
            var foo = new TestSample();
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

        private static async UniTask ExecuteTest(TestCondition condition)
        {
            var target = new TestSample();

            var delegates = condition.Delegates;
            var ct = condition.CancellationToken;
            var taskStatus = condition.TaskStatus;
            var assertion = condition.Assertion;

            Assert.Equal(0, target.SubscibedCount);
            var unsubscribers = new List<AsyncEventUnsubscriber<TestSample>>();
            foreach(var d in delegates) {
                var unsubscriber = target.Test.Subscribe(d);
                unsubscribers.Add(unsubscriber);
            }

            Assert.Equal(delegates.Length, target.SubscibedCount);

            var raisedEventTask = target.SequentiallyRaiseTest(ct);
            if(taskStatus != null) {
                Assert.Equal(taskStatus, raisedEventTask.Status);
            }

            if(taskStatus == UniTaskStatus.Canceled) {
                await Assert.ThrowsAsync<OperationCanceledException>(async () => await raisedEventTask);
            }
            else {
                await raisedEventTask;
            }


            Assert.Equal(delegates.Length, target.SubscibedCount);
            assertion(target);

            unsubscribers.ForEach(u => u.Dispose());
            unsubscribers.Clear();
            Assert.Equal(0, target.SubscibedCount);
            assertion(target);
        }

        private sealed class TestCondition
        {
            public Func<TestSample, CancellationToken, UniTask>[] Delegates { get; init; } = Array.Empty<Func<TestSample, CancellationToken, UniTask>>();
            public CancellationToken CancellationToken { get; init; }
            public UniTaskStatus? TaskStatus { get; init; }
            public Action<TestSample> Assertion { get; init; } = _ => { };
        }

        private sealed class TestSample
        {
            private AsyncEventRaiser<TestSample>? _test;

            public AsyncEvent<TestSample> Test => new AsyncEvent<TestSample>(ref _test);

            public int SubscibedCount => _test?.SubscibedCount ?? 0;


            public int Value { get; set; }


            public UniTask SequentiallyRaiseTest(CancellationToken ct)
            {
                return _test.RaiseSequentiallyIfNotNull(this, ct);
            }
        }
    }
}
