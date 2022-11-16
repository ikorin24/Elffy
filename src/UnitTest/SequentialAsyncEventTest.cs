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
    public class SequentialAsyncEventTest
    {
        private static readonly Func<TestSample, CancellationToken, UniTask> Sync_IncrementValue = (sender, ct) =>
        {
            sender.Value++;
            return UniTask.CompletedTask;
        };

        private static readonly Func<TestSample, CancellationToken, UniTask> Sync_ShouldNotBeCalled = (sender, ct) =>
        {
            Assert.True(false, "No one should not come here.");
            return UniTask.CompletedTask;
        };

        private static readonly Func<TestSample, CancellationToken, UniTask> Async_ShouldNotBeCalled = async (sender, ct) =>
        {
            Assert.True(false, "No one should not come here.");
            await UniTask.CompletedTask;
        };

        private static readonly Func<TestSample, CancellationToken, UniTask> Async_Add5_Add1 = async (sender, ct) =>
        {
            sender.Value += 5;
            await Task.Delay(10, ct).ConfigureAwait(false);
            sender.Value += 1;
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
            var condition = new TestCondition()
            {
                Delegates = new[] { Async_Add5_Add1, },
                CancellationToken = CancellationToken.None,
                TaskStatus = null,
                Assertion = target => Assert.Equal(6, target.Value),
            };
            await ExecuteTest(condition);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(20)]
        public async Task MultiSubscribe(int delegateCount)
        {
            var array = new int[delegateCount];
            var unsubscribers = new AsyncEventSubscription<TestSample>[delegateCount];
            var foo = new TestSample();
            Assert.Equal(0, foo.SubscibedCount);

            for(int i = 0; i < delegateCount; i++) {
                var num = i;
                if(i % 2 == 0) {
                    unsubscribers[i] = foo.Test.Subscribe(async (sender, ct) =>
                    {
                        array[num] += 5;
                        await Task.Delay(10, ct).ConfigureAwait(false);
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
                Delegates = null,
                CancellationToken = new CancellationToken(true),
                TaskStatus = UniTaskStatus.Canceled,
                Assertion = target => Assert.Equal(0, target.Value),
            };
            await ExecuteTest(condition);
        }

        [Fact]
        public async Task AlreadyCanceled_AsyncDelegate()
        {
            var condition = new TestCondition()
            {
                Delegates = new[] { Async_ShouldNotBeCalled, },
                CancellationToken = new CancellationToken(true),
                TaskStatus = UniTaskStatus.Canceled,
                Assertion = target => Assert.Equal(0, target.Value),
            };
            await ExecuteTest(condition);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(20)]
        public async Task AlreadyCanceled_MultiSubscribed(int delegateCount)
        {
            var delegates = Enumerable
                .Range(0, delegateCount)
                .Select(i => (i % 2 == 0) ? Async_ShouldNotBeCalled : Sync_ShouldNotBeCalled)
                .ToArray();
            var condition = new TestCondition()
            {
                Delegates = delegates,
                CancellationToken = new CancellationToken(true),
                TaskStatus = UniTaskStatus.Canceled,
                Assertion = target => Assert.Equal(0, target.Value),
            };
            await ExecuteTest(condition);
        }

        private static async UniTask ExecuteTest(TestCondition condition)
        {
            var target = new TestSample();

            var delegates = condition.Delegates ?? new Func<TestSample, CancellationToken, UniTask>[0];
            var ct = condition.CancellationToken;
            var taskStatus = condition.TaskStatus;
            var assertion = condition.Assertion;

            Assert.Equal(0, target.SubscibedCount);
            var unsubscribers = new List<AsyncEventSubscription<TestSample>>();
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
            assertion?.Invoke(target);

            unsubscribers.ForEach(u => u.Dispose());
            unsubscribers.Clear();
            Assert.Equal(0, target.SubscibedCount);
            assertion?.Invoke(target);
        }

        private sealed class TestCondition
        {
            public Func<TestSample, CancellationToken, UniTask>[]? Delegates { get; init; }
            public CancellationToken CancellationToken { get; init; }
            public UniTaskStatus? TaskStatus { get; init; }
            public Action<TestSample>? Assertion { get; init; }
        }

        private sealed class TestSample
        {
            private AsyncEventSource<TestSample>? _test;

            public AsyncEvent<TestSample> Test => new AsyncEvent<TestSample>(ref _test);

            public int SubscibedCount => _test?.SubscibedCount ?? 0;


            public int Value { get; set; }

            public UniTask SequentiallyRaiseTest(CancellationToken ct)
            {
                return _test.InvokeSequentiallyIfNotNull(this, ct);
            }
        }
    }
}
