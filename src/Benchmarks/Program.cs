#nullable enable
using BenchmarkDotNet.Running;

namespace Benchmarks.Events
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<EventBenchmark>();
        }
    }
}
