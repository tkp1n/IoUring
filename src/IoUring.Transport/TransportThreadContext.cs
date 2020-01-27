using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using static Tmds.Linux.LibC;

namespace IoUring.Transport
{
    internal class TransportThreadContext
    {
        private readonly int _eventFd;
        private bool _unsafeBlockingMode;
        
        public TransportThreadContext(ConcurrentQueue<IoUringConnectionContext> readPollQueue, ConcurrentQueue<IoUringConnectionContext> writePollQueue, int eventFd)
        {
            ReadPollQueue = readPollQueue;
            WritePollQueue = writePollQueue;
            _eventFd = eventFd;
        }

        public bool UnsafeBlockingMode => _unsafeBlockingMode;

        public bool BlockingMode
        {
            get => Volatile.Read(ref _unsafeBlockingMode);
            set => Volatile.Write(ref _unsafeBlockingMode, value);
        }

        /// <summary>
        /// Unblocks the thread
        /// </summary>
        public unsafe void Unblock()
        {
            byte* val = stackalloc byte[sizeof(ulong)];
            Unsafe.WriteUnaligned(val, 1UL);
            if (write(_eventFd, val, sizeof(ulong)) == -1)
            {
                throw new ErrnoException(errno);
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