using IoUring.Internal;

namespace IoUring.Concurrent
{
    /// <summary>
    /// Factory for <see cref="ConcurrentRing"/> instances with a shared work queue
    /// </summary>
    /// <remarks>Available since 5.6</remarks>
    public class SharedConcurrentRingWorkQueue
    {
        private readonly SharedWorkQueue<ConcurrentRing> _sharedWq = new SharedWorkQueue<ConcurrentRing>();

        /// <summary>
        /// Returns whether shared work queues are supported by the kernel
        /// </summary>
        public static bool IsSupported => SharedWorkQueue<ConcurrentRing>.IsSupported;

        /// <summary>
        /// Creates a new instance of <see cref="ConcurrentRing"/> with a shared work queue
        /// </summary>
        public ConcurrentRing Create(int entries, RingOptions? options = default)
            => _sharedWq.Create(entries, options ?? new RingOptions(), (e, o) =>
            {
                var r = new ConcurrentRing(entries, o);
                return (r.FileHandle, r);
            });
    }
}