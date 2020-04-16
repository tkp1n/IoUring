using System;
using static IoUring.Internal.ThrowHelper;

namespace IoUring.Internal
{
    internal class SharedWorkQueue<T> where T : BaseRing
    {
        private int _ringFd;

        public SharedWorkQueue()
        {
            if (!KernelVersion.Supports.IORING_SETUP_ATTACH_WQ) ThrowPlatformNotSupportedException();
        }

        public T Create(int entries, RingOptions options, Func<int, RingOptions, T> activator)
        {
            if (_ringFd == 0)
            {
                lock (this)
                {
                    if (_ringFd == 0)
                    {
                        var ring = activator(entries, options);
                        _ringFd = ring.FileHandle;
                        return ring;
                    }
                }
            }

            options.WorkQueueFd = _ringFd;
            return activator(entries, options);
        }
    }
}