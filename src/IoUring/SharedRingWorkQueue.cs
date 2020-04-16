using IoUring.Internal;

namespace IoUring
{
    public class SharedRingWorkQueue
    {
        private readonly SharedWorkQueue<Ring> _sharedWq = new SharedWorkQueue<Ring>();

        public Ring Create(int entries)
            => _sharedWq.Create(entries, new RingOptions(), (e, o) => new Ring(entries, o));

        public Ring Create(int entries, RingOptions options)
            => _sharedWq.Create(entries, options ?? new RingOptions(), (e, o) => new Ring(entries, o));
    }
}