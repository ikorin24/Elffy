#nullable enable
using Cysharp.Threading.Tasks;
using Xunit;

namespace UnitTest
{
    [Collection("UseEngine")]
    public sealed class EngineRunTest
    {
        [Fact]
        public static void StartTest() => TestEngineEntryPoint.Start(screen => UniTask.CompletedTask);
    }
}
