#nullable enable
using Elffy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using UniTask = Cysharp.Threading.Tasks.UniTask;

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
            using(var unsubscribers = new UnsubscriberBag()) {
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
            using(var unsubscribers = new UnsubscriberBag()) {
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
            using(var unsubscribers = new UnsubscriberBag()) {
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
            using(var unsubscribers = new UnsubscriberBag()) {
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
            using(var unsubscribers = new UnsubscriberBag()) {

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

        [Fact]
        public async Task OrderTest()
        {
            var called = new List<int>();
            var awaitHelper = new AwaitHelper("0");

            var sample = new Sample();
            using(var unsbscribers = new UnsubscriberBag()) {
                sample.TestEvent.Subscribe(async (x, ct) =>
                {
                    await Task.Delay(30, CancellationToken.None);
                    called.Add(0);
                    awaitHelper.ChangeState("1", out var before);
                    Assert.Equal("0", before);
                }).AddTo(unsbscribers);

                sample.TestEvent.Subscribe(async (x, ct) =>
                {
                    await awaitHelper.WaitUntil("1");
                    called.Add(1);
                    awaitHelper.ChangeState("2", out var before);
                    Assert.Equal("1", before);
                }).AddTo(unsbscribers);

                sample.TestEvent.Subscribe(async (x, ct) =>
                {
                    await awaitHelper.WaitUntil("2");
                    called.Add(2);
                    awaitHelper.ChangeState("3", out var before);
                    Assert.Equal("2", before);
                }).AddTo(unsbscribers);

                sample.TestEvent.Subscribe(async (x, ct) =>
                {
                    await awaitHelper.WaitUntil("3");
                    called.Add(3);
                    awaitHelper.ChangeState("end", out var before);
                    Assert.Equal("3", before);
                }).AddTo(unsbscribers);

                await sample.ParallelRaiseTest(CancellationToken.None);

                Assert.Equal("end", awaitHelper.State);
                Assert.True(called.SequenceEqual(new int[] { 0, 1, 2, 3, }));
            }

            Assert.Equal(0, sample.SubscibedCount);
        }

        private sealed class Sample
        {
            private AsyncEventSource<Sample>? _testEvent;

            public AsyncEvent<Sample> TestEvent => new AsyncEvent<Sample>(ref _testEvent);

            public int SubscibedCount => _testEvent?.SubscibedCount ?? 0;


            public UniTask ParallelRaiseTest(CancellationToken ct)
            {
                return _testEvent.InvokeIfNotNull(this, ct);
            }
        }

        private sealed class AwaitHelper
        {
            private string _state;

            public string State => _state;

            public AwaitHelper(string state)
            {
                _state = state;
            }

            public void ChangeState(string state, out string before)
            {
                (before, _state) = (_state, state);
            }

            public UniTask WaitUntil(string value)
            {
                if(_state == value) {
                    return UniTask.CompletedTask;
                }
                return Loop(value);
            }

            private async UniTask Loop(string value)
            {
                while(true) {
                    if(_state == value) { return; }
                    await Task.Delay(1);
                }
            }
        }
    }
}
