#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Cysharp.Threading.Tasks;
using Elffy.Threading;
using System.Linq;

namespace UnitTest
{
    public class ParallelOperationTest
    {
        [Theory]
        [InlineData(1, 20)]
        [InlineData(2, 20)]
        [InlineData(3, 20)]
        [InlineData(4, 20)]
        [InlineData(5, 20)]
        [InlineData(8, 20)]
        [InlineData(10, 20)]
        [InlineData(11, 20)]
        [InlineData(15, 20)]
        [InlineData(20, 20)]
        public async Task LimitedParallel(int maxParallel, int operationCount)
        {
            // Test for the number of functions called at the same time is `maxParallel`,
            // and ensure that all functions are called only once.

            var counter = new ParallelCallCounter(maxParallel);
            var funcCalled = new int[operationCount];
            var funcs = new Func<ParallelCallCounter, CancellationToken, UniTask>[operationCount];
            for(int i = 0; i < funcs.Length; i++) {
                var funcNum = i;
                funcs[i] = async (counter, ct) =>
                {
                    await TestWork(counter, ct);
                    Assert.Equal(1, Interlocked.Increment(ref funcCalled[funcNum]));
                };
            }
            await ParallelOperation.LimitedParallel(funcs, counter, maxParallel, CancellationToken.None);
            counter.Ensure();
            Assert.True(funcCalled.All(x => x == 1));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(8)]
        [InlineData(10)]
        [InlineData(11)]
        [InlineData(15)]
        [InlineData(20)]
        public async Task EmptyTest(int operationCount)
        {
            // Test that the method throws an exception if maxParallel == 0.
            const int MaxParallel = 0;

            var counter = new ParallelCallCounter(MaxParallel);
            var funcs = new Func<ParallelCallCounter, CancellationToken, UniTask>[operationCount];
            for(int i = 0; i < funcs.Length; i++) {
                funcs[i] = (counter, ct) =>
                {
                    throw new Exception("This task should not be called !!");
                };
            }
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                await ParallelOperation.LimitedParallel(funcs, counter, MaxParallel, CancellationToken.None);
            });
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(8)]
        [InlineData(10)]
        [InlineData(11)]
        [InlineData(15)]
        [InlineData(20)]
        public async Task ExceptionTest(int operationCount)
        {
            // Test that the method throws an exception if maxParallel < 0.
            const int MaxParallel = 0;

            var counter = new ParallelCallCounter(MaxParallel);
            var funcs = new Func<ParallelCallCounter, CancellationToken, UniTask>[operationCount];
            for(int i = 0; i < funcs.Length; i++) {
                funcs[i] = (counter, ct) =>
                {
                    throw new Exception("This task should not be called !!");
                };
            }
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                await ParallelOperation.LimitedParallel(funcs, counter, -1, CancellationToken.None);
            });
        }

        private static async UniTask TestWork(ParallelCallCounter counter, CancellationToken ct)
        {
            await UniTask.SwitchToThreadPool();
            counter.Enter();
            await Task.Delay(100);
            counter.Exit();
        }

        private sealed class ParallelCallCounter
        {
            private int _parallelCall;
            private int _max;

            public ParallelCallCounter(int max)
            {
                _max = max;
                _parallelCall = 0;
            }

            public void Ensure()
            {
                Assert.Equal(0, _parallelCall);
            }

            public void Enter()
            {
                if(Interlocked.Increment(ref _parallelCall) > _max) {
                    throw new Exception($"Max limit was exceeded. (max: {_max})");
                }
            }

            public void Exit()
            {
                Interlocked.Decrement(ref _parallelCall);
            }
        }
    }
}
