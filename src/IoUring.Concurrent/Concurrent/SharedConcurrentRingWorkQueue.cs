using IoUring.Internal;

namespace IoUring.Concurrent
{
    public class SharedConcurrentRingWorkQueue
    {
        private readonly SharedWorkQueue<ConcurrentRing> _sharedWq = new SharedWorkQueue<ConcurrentRing>();

        public ConcurrentRing Create(int entries)
            => _sharedWq.Create(entries, new RingOptions(), (e, o) => new ConcurrentRing(entries, o));

        public ConcurrentRing Create(int entries, RingOptions options)
            => _sharedWq.Create(entries, options ?? new RingOptions(), (e, o) => new ConcurrentRing(entries, o));
    }
}