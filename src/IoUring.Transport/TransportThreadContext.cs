using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using static Tmds.Linux.LibC;

namespace IoUring.Transport
{
    internal class TransportThreadContext
    {
        private volatile bool _isBlocked;
        private readonly int _eventFd;
        
        public TransportThreadContext(ConcurrentQueue<IoUringConnectionContext> readPollQueue, ConcurrentQueue<IoUringConnectionContext> writePollQueue, int eventFd)
        {
            ReadPollQueue = readPollQueue;
            WritePollQueue = writePollQueue;
            _eventFd = eventFd;
        }

        /// <summary>
        /// Gets/Sets whether the thread is blocked in the io_uring 
        /// </summary>
        public bool IsBlocked
        {
            get => _isBlocked;
            set => _isBlocked = value;
        }

        /// <summary>
        /// Unblocks the thread
        /// </summary>
        public unsafe void Unblock()
        {
            bool blocked;
            if ((blocked = _isBlocked) == true)
            {
                // Spin first to avoid syscall if thread unblocks itself during the spins
                var sw = new SpinWait();
                while (!sw.NextSpinWillYield && (blocked = _isBlocked) == true)
                {
                    sw.SpinOnce();
                }

                if (blocked)
                {
                    // Thread is still blocked, actually unblock the thread by triggering the read on the eventFd
                    byte* val = stackalloc byte[sizeof(ulong)];
                    Unsafe.WriteUnaligned(val, 1UL);
                    if (write(_eventFd, val, sizeof(ulong)) == -1)
                    {
                        throw new ErrnoException(errno);
                    }
                }
            }
        }

        /// <summary>
        /// Queue of connections for which a poll for available data to read should be registered
        /// </summary>
        public ConcurrentQueue<IoUringConnectionContext> ReadPollQueue { get; }

        /// <summary>
        /// Queue of connections for which a poll for a possible write operation should be registered
        /// </summary>
        public ConcurrentQueue<IoUringConnectionContext> WritePollQueue { get; }
    }
}