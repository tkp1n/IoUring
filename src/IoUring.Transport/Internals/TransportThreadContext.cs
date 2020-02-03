using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using IoUring.Transport.Internals.Metrics;
using static Tmds.Linux.LibC;

namespace IoUring.Transport.Internals
{
    internal sealed class TransportThreadContext
    {
        private readonly int _eventFd;
        private bool _unsafeBlockingMode;

        public TransportThreadContext(IoUringOptions options, ConcurrentQueue<IoUringConnectionContext> readPollQueue,
            ConcurrentQueue<IoUringConnectionContext> writePollQueue, MemoryPool<byte> memoryPool, int eventFd)
        {
            ReadPollQueue = readPollQueue;
            WritePollQueue = writePollQueue;
            _eventFd = eventFd;
            Options = options;
            MemoryPool = memoryPool;
        }

        public bool UnsafeBlockingMode => _unsafeBlockingMode;

        public bool BlockingMode
        {
            get => Volatile.Read(ref _unsafeBlockingMode);
            set => Volatile.Write(ref _unsafeBlockingMode, value);
        }

        public IoUringOptions Options { get; }

        public ConcurrentQueue<IoUringConnectionContext> ReadPollQueue { get; }

        public ConcurrentQueue<IoUringConnectionContext> WritePollQueue { get; }

        public MemoryPool<byte> MemoryPool { get; }

        public unsafe void Notify()
        {
            if (!BlockingMode)
            {
                // If the transport thread is not (yet) in blocking mode, we have the guarantee, that it will read 
                // from the queues one more time before actually blocking. Therefore, it is safe not to notify now.
                return;
            }

            // The transport thread reported he is (probably still) blocking. We therefore must notify it via writing
            // to the eventfd established for that purpose.

            IoUringTransportEventSource.Log.ReportEventFdWrite();
            Debug.WriteLine("Attempting to unblock thread");

            byte* val = stackalloc byte[sizeof(ulong)];
            Unsafe.WriteUnaligned(val, 1UL);
            if (write(_eventFd, val, sizeof(ulong)) == -1)
            {
                throw new ErrnoException(errno);
            }
        }
    }
}