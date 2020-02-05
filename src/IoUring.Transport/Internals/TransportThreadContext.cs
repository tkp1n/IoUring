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
        private const int True = 1;
        private const int False = 0;
        
        private readonly int _eventFd;
        private int _blockingMode;

        public TransportThreadContext(IoUringOptions options, ConcurrentQueue<IoUringConnectionContext> readPollQueue,
            ConcurrentQueue<IoUringConnectionContext> writePollQueue, MemoryPool<byte> memoryPool, int eventFd)
        {
            ReadPollQueue = readPollQueue;
            WritePollQueue = writePollQueue;
            _eventFd = eventFd;
            Options = options;
            MemoryPool = memoryPool;
        }

        public bool BlockingMode
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref _blockingMode) == True;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Volatile.Write(ref _blockingMode, value ? True : False);
        }

        public IoUringOptions Options { get; }

        public ConcurrentQueue<IoUringConnectionContext> ReadPollQueue { get; }

        public ConcurrentQueue<IoUringConnectionContext> WritePollQueue { get; }

        public MemoryPool<byte> MemoryPool { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ShouldUnblock() => Interlocked.CompareExchange(ref _blockingMode, False, True) == True;

        public unsafe void Notify()
        {
            if (!ShouldUnblock())
            {
                // If the transport thread is not (yet) in blocking mode, we have the guarantee, that it will read 
                // from the queues one more time before actually blocking. Therefore, it is safe not to notify now.
                return;
            }

            // The transport thread reported it is (probably still) blocking. We therefore must notify it by writing
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