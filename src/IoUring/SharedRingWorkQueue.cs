using IoUring.Internal;

namespace IoUring
{
    /// <summary>
    /// Factory for <see cref="Ring"/> instances with a shared work queue
    /// </summary>
    /// <remarks>Available since 5.6</remarks>
    public class SharedRingWorkQueue
    {
        private readonly SharedWorkQueue<Ring> _sharedWq = new SharedWorkQueue<Ring>();

        /// <summary>
        /// Returns whether shared work queues are supported by the kernel
        /// </summary>
        public static bool IsSupported => SharedWorkQueue<Ring>.IsSupported;

        /// <summary>
        /// Creates a new instance of <see cref="Ring"/> with a shared work queue
        /// </summary>
        public Ring Create(int entries, RingOptions? options = default)
            => _sharedWq.Create(entries, options ?? new RingOptions(), (e, o) =>
            {
                var r = new Ring(entries, o);
                return (r.FileHandle, r);
            });
    }
}