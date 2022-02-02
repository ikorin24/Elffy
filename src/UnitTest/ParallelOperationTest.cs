#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Cysharp.Threading.Tasks;
using Elffy.Threading;

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
            var testState = new TestState(maxParallel);
            var funcs = new Func<TestState, CancellationToken, UniTask>[operationCount];
            for(int i = 0; i < funcs.Length; i++) {
                funcs[i] = (s, ct) => TestWork(s, ct);
            }
            await ParallelOperation.LimitedParallel(funcs, testState, maxParallel, CancellationToken.None);
            testState.Ensure();
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
            const int MaxParallel = 0;

            var testState = new TestState(MaxParallel);
            var funcs = new Func<TestState, CancellationToken, UniTask>[operationCount];
            for(int i = 0; i < funcs.Length; i++) {
                funcs[i] = (s, ct) => TestWork(s, ct);
            }
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                await ParallelOperation.LimitedParallel(funcs, testState, MaxParallel, CancellationToken.None);
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
            const int MaxParallel = 0;

            var testState = new TestState(MaxParallel);
            var funcs = new Func<TestState, CancellationToken, UniTask>[operationCount];
            for(int i = 0; i < funcs.Length; i++) {
                funcs[i] = (s, ct) => TestWork(s, ct);
            }
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            {
                await ParallelOperation.LimitedParallel(funcs, testState, -1, CancellationToken.None);
            });
        }

        private static async UniTask TestWork(TestState testState, CancellationToken ct)
        {
            await UniTask.SwitchToThreadPool();
            testState.Enter();
            await Task.Delay(100);
            testState.Exit();
        }

        private sealed class TestState
        {
            private int _value;
            private int _max;

            public TestState(int max)
            {
                _max = max;
                _value = 0;
            }

            public void Ensure()
            {
                Assert.Equal(0, _value);
            }

            public void Enter()
            {
                if(Interlocked.Increment(ref _value) > _max) {
                    throw new Exception("Test failure");
                }
            }

            public void Exit()
            {
                Interlocked.Decrement(ref _value);
            }
        }
    }
}
