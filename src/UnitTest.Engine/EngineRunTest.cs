#nullable enable
using System;
using Cysharp.Threading.Tasks;
using Xunit;

namespace UnitTest
{
    [Collection(TestEngineEntryPoint.UseEngineSymbol)]
    public sealed class EngineRunTest
    {
        [Fact]
        public static void StartTest() => TestEngineEntryPoint.Start(screen => UniTask.CompletedTask);
    }
}
