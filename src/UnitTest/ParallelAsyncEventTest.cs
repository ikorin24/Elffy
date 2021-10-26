#nullable enable
using Cysharp.Threading.Tasks;
using Elffy;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace UnitTest
{
    public class ParallelAsyncEventTest
    {
        [Theory]
        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(3, false)]
        [InlineData(4, false)]
        [InlineData(20, false)]
        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(2, true)]
        [InlineData(3, true)]
        [InlineData(4, true)]
        [InlineData(20, true)]
        public async Task Sync_Completed_Raise(int delegateCount, bool alreadyCanceled)
        {
            var sample = new Sample();
            using(var unsubscribers = new Unsbscribers<Sample>()) {
                for(int i = 0; i < delegateCount; i++) {
                    sample.TestEvent.Subscribe((x, ct) => UniTask.CompletedTask).AddTo(unsubscribers);
                }
                Assert.Equal(delegateCount, sample.SubscibedCount);
                if(alreadyCanceled) {
                    await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                    {
                        await sample.ParallelRaiseTest(new CancellationToken(true));
                    });
                }
                else {
                    await sample.ParallelRaiseTest(CancellationToken.None);
                }
            }
            Assert.Equal(0, sample.SubscibedCount);
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(3, false)]
        [InlineData(4, false)]
        [InlineData(20, false)]
        [InlineData(0, true)]
        [InlineData(1, true)]
        [InlineData(2, true)]
        [InlineData(3, true)]
        [InlineData(4, true)]
        [InlineData(20, true)]
        public async Task Async_Completed_Raise(int delegateCount, bool alreadyCanceled)
        {
            var sample = new Sample();
            using(var unsubscribers = new Unsbscribers<Sample>()) {
                for(int i = 0; i < delegateCount; i++) {
                    sample.TestEvent.Subscribe(async (x, ct) => await UniTask.CompletedTask).AddTo(unsubscribers);
                }
                Assert.Equal(delegateCount, sample.SubscibedCount);
                if(alreadyCanceled) {
                    await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                    {
                        await sample.ParallelRaiseTest(new CancellationToken(true));
                    });
                }
                else {
                    await sample.ParallelRaiseTest(CancellationToken.None);
                }
            }
            Assert.Equal(0, sample.SubscibedCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(20)]
        public async Task Sync_NeverCompleted_Raise(int delegateCount)
        {
            var sample = new Sample();
            using(var unsubscribers = new Unsbscribers<Sample>()) {
                for(int i = 0; i < delegateCount; i++) {
                    sample.TestEvent.Subscribe((x, ct) => UniTask.Never(ct)).AddTo(unsubscribers);
                }
                Assert.Equal(delegateCount, sample.SubscibedCount);
                await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                {
                    await sample.ParallelRaiseTest(new CancellationToken(true));
                });
            }
            Assert.Equal(0, sample.SubscibedCount);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(20)]
        public async Task Async_NeverCompleted_Raise(int delegateCount)
        {
            var sample = new Sample();
            using(var unsubscribers = new Unsbscribers<Sample>()) {
                for(int i = 0; i < delegateCount; i++) {
                    sample.TestEvent.Subscribe(async (x, ct) => await UniTask.Never(ct)).AddTo(unsubscribers);
                }
                Assert.Equal(delegateCount, sample.SubscibedCount);
                await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                {
                    await sample.ParallelRaiseTest(new CancellationToken(true));
                });
            }
            Assert.Equal(0, sample.SubscibedCount);
        }

        [Fact]
        public async Task Async_CancelDuringExecution_Raise()
        {
            var sample = new Sample();
            var cts = new CancellationTokenSource();
            using(var unsubscribers = new Unsbscribers<Sample>()) {

                sample.TestEvent.Subscribe(async (x, ct) => await RunUntilCanceled(ct)).AddTo(unsubscribers);
                sample.TestEvent.Subscribe(async (x, ct) => await RunUntilCanceled(ct)).AddTo(unsubscribers);

                sample.TestEvent.Subscribe(async (x, ct) =>
                {
                    await Task.Delay(10, ct);
                    cts.Cancel();
                }).AddTo(unsubscribers);

                sample.TestEvent.Subscribe(async (x, ct) => await RunUntilCanceled(ct)).AddTo(unsubscribers);
                sample.TestEvent.Subscribe(async (x, ct) => await RunUntilCanceled(ct)).AddTo(unsubscribers);

                Assert.Equal(5, sample.SubscibedCount);
                await Assert.ThrowsAsync<OperationCanceledException>(async () =>
                {
                    await sample.ParallelRaiseTest(cts.Token);
                });
            }
            Assert.Equal(0, sample.SubscibedCount);
            return;

            static async UniTask RunUntilCanceled(CancellationToken ct)
            {
                while(true) {
                    ct.ThrowIfCancellationRequested();
                    await Task.Delay(1, CancellationToken.None);
                }
            }
        }

        private sealed class Sample
        {
            private AsyncEventRaiser<Sample>? _testEvent;

            public AsyncEvent<Sample> TestEvent => new AsyncEvent<Sample>(ref _testEvent);

            public int SubscibedCount => _testEvent?.SubscibedCount ?? 0;


            public UniTask ParallelRaiseTest(CancellationToken ct)
            {
                return _testEvent.RaiseParallelIfNotNull(this, ct);
            }
        }
    }

    internal sealed class Unsbscribers<T> : IDisposable
    {
        private readonly List<AsyncEventUnsubscriber<T>> _list = new List<AsyncEventUnsubscriber<T>>();
        public void Add(AsyncEventUnsubscriber<T> unsubscriber)
        {
            _list.Add(unsubscriber);
        }

        public void Dispose()
        {
            foreach(var u in _list) {
                u.Dispose();
            }
            _list.Clear();
        }
    }

    internal static class AsyncEvnetUnsbscriberExtension
    {
        public static void AddTo<T>(this AsyncEventUnsubscriber<T> unsubscriber, Unsbscribers<T> unsbscribers)
        {
            unsbscribers.Add(unsubscriber);
        }
    }
}
