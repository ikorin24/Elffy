#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Elffy.Threading;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace UnitTest
{
    public class ParallelTest
    {
        [Fact]
        public async Task For()
        {
            const int ParallelCount = 30;
            var counter = 0;

            await ParallelSchedule.For(ParallelCount, () =>
            {
                Interlocked.Increment(ref counter);
            });

            Assert.True(counter == ParallelCount);
        }

        [Fact]
        public async Task Parallel()
        {
            for(int i = 0; i < 50; i++) {
                await ExecuteParallel(i);
            }
        }

        [Fact]
        public async Task ParallelMany()
        {
            await ExecuteParallel(1000);
        }

        [Fact]
        public async Task ForWithArg()
        {
            const int ParallelCount = 30;
            var counter = 0;

            await ParallelSchedule.For(ParallelCount, "aaa", arg =>
            {
                Interlocked.Increment(ref counter);
                Assert.Equal("aaa", arg);
            });

            Assert.True(counter == ParallelCount);
        }

        private static async UniTask ExecuteParallel(int parallelCount)
        {
            var callerThreadId = Environment.CurrentManagedThreadId;
            var flags = new bool[parallelCount];
            var actions = Enumerable.Range(0, parallelCount).Select<int, Action>(i => () =>
            {
                Assert.False(flags[i]);
                Assert.True(callerThreadId != Environment.CurrentManagedThreadId);
                flags[i] = true;
            }).ToArray();

            await ParallelSchedule.Parallel(actions);
            Assert.True(flags.All(f => f));
        }
    }
}
