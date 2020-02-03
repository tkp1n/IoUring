using System.Buffers;
using System.Collections.Concurrent;
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

        public unsafe void Unblock()
        {
            IoUringTransportEventSource.Log.ReportEventFdWrite();

            byte* val = stackalloc byte[sizeof(ulong)];
            Unsafe.WriteUnaligned(val, 1UL);
            if (write(_eventFd, val, sizeof(ulong)) == -1)
            {
                throw new ErrnoException(errno);
            }
        }
    }
}