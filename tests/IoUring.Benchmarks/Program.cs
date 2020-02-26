using BenchmarkDotNet.Running;

namespace IoUring.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<RingBenchmark>();
        }
    }
}
