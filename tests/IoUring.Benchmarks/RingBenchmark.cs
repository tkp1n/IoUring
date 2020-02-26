using BenchmarkDotNet.Attributes;

namespace IoUring.Benchmarks
{
    public class RingBenchmark
    {
        private Ring _ring;

        private const int RingSize = 4096;

        [GlobalSetup]
        public void Setup()
        {
            _ring = new Ring(RingSize);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _ring.Dispose();
        }

        [Benchmark]
        public void PerpSubmitRead()
        {
            _ring.TryPrepareNop();
            _ring.Submit(out _);
            _ring.TryRead(out _);
        }
    }
}