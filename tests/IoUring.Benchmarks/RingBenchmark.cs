using BenchmarkDotNet.Attributes;
using IoUring.Concurrent;

namespace IoUring.Benchmarks
{
    public class RingBenchmark
    {
        private Ring _ring;
        private ConcurrentRing _concurrentRing;

        private const int RingSize = 4096;

        [GlobalSetup]
        public void Setup()
        {
            _ring = new Ring(RingSize);
            _concurrentRing = new ConcurrentRing(RingSize);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _ring.Dispose();
            _concurrentRing.Dispose();
        }

        [Benchmark]
        public void PerpSubmitRead()
        {
            _ring.TryPrepareNop();
            _ring.Submit(out _);
            _ring.TryRead(out _);
        }

        [Benchmark]
        public void PrepSubmitReadConcurrent()
        {
            _concurrentRing.TryAcquireSubmission(out var s);
            s.PrepareNop();
            _concurrentRing.Release(s);
            _concurrentRing.Submit(out _);
            _concurrentRing.TryRead(out _);
        }
    }
}