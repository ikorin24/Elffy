#nullable enable
using Cysharp.Threading.Tasks;
using Elffy.Threading;
using System;
using System.Threading.Tasks;
using Xunit;

namespace UnitTest
{
    public class TaskExtensionTest
    {
        [Fact]
        public void SyncGetResult()
        {
            // It must complete the task synchronously.
            {
                UniTask.CompletedTask.SyncGetResult();
                Assert.Equal(0, new UniTask<int>(0).SyncGetResult());
                Assert.Equal("aaa", new UniTask<string>("aaa").SyncGetResult());
                Assert.Equal(AsyncUnit.Default, new UniTask<AsyncUnit>(AsyncUnit.Default).SyncGetResult());
            }
            {
                ValueTask.CompletedTask.SyncGetResult();
                Assert.Equal(0, new ValueTask<int>(0).SyncGetResult());
                Assert.Equal("aaa", new ValueTask<string>("aaa").SyncGetResult());
                Assert.Equal(AsyncUnit.Default, new ValueTask<AsyncUnit>(AsyncUnit.Default).SyncGetResult());
            }
            {
                Task.CompletedTask.SyncGetResult();
                Assert.Equal(0, new ValueTask<int>(0).AsTask().SyncGetResult());
                Assert.Equal("aaa", new ValueTask<string>("aaa").AsTask().SyncGetResult());
                Assert.Equal(AsyncUnit.Default, new ValueTask<AsyncUnit>(AsyncUnit.Default).AsTask().SyncGetResult());
            }

            // It must throw an exception when we try to get result.
            {
                Assert.Throws<InvalidOperationException>(() => UniTask.Never(default).SyncGetResult());
                Assert.Throws<InvalidOperationException>(() => UniTask.Never<int>(default).SyncGetResult());
                Assert.Throws<InvalidOperationException>(() => UniTask.Never<string>(default).SyncGetResult());
                Assert.Throws<InvalidOperationException>(() => UniTask.Never<AsyncUnit>(default).SyncGetResult());
            }
            {
                Assert.Throws<InvalidOperationException>(() => UniTask.Never(default).AsValueTask().SyncGetResult());
                Assert.Throws<InvalidOperationException>(() => UniTask.Never<int>(default).AsValueTask().SyncGetResult());
                Assert.Throws<InvalidOperationException>(() => UniTask.Never<string>(default).AsValueTask().SyncGetResult());
                Assert.Throws<InvalidOperationException>(() => UniTask.Never<AsyncUnit>(default).AsValueTask().SyncGetResult());
            }
            {
                Assert.Throws<InvalidOperationException>(() => UniTask.Never(default).AsTask().SyncGetResult());
                Assert.Throws<InvalidOperationException>(() => UniTask.Never<int>(default).AsTask().SyncGetResult());
                Assert.Throws<InvalidOperationException>(() => UniTask.Never<string>(default).AsTask().SyncGetResult());
                Assert.Throws<InvalidOperationException>(() => UniTask.Never<AsyncUnit>(default).AsTask().SyncGetResult());
            }
        }
    }
}
