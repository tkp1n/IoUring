using System;
using System.Threading;
using Tmds.Linux;
using static Tmds.Linux.LibC;
using static IoUring.Internal.Helpers;
using static IoUring.Internal.ThrowHelper;

namespace IoUring.Internal
{
    internal unsafe class CompletionQueue
    {
        /// <summary>
        /// Incremented by the application to let the kernel know, which Completion Queue Events were already consumed.
        /// </summary>
        private uint* _head;

        /// <summary>
        /// Incremented by the kernel to let the application know about another Completion Queue Event.
        /// </summary>
        private uint* _tail;

        /// <summary>
        /// Mask to apply to potentially overflowing head counter to get a valid index within the ring.
        /// </summary>
        private uint* _ringMask;

        /// <summary>
        /// Number of entries in the ring.
        /// </summary>
        private uint* _ringEntries;

        /// <summary>
        /// Incremented by the kernel on each overwritten Completion Queue Event.
        /// This is a sign, that the application is producing Submission Queue Events faster as it handles the corresponding Completion Queue Events.
        /// </summary>
        private uint* _overflow;

        /// <summary>
        /// Completion Queue Events filled by the kernel.
        /// </summary>
        private io_uring_cqe* _cqes;

        private uint* _headInternal;

        private uint* _tailInternal;

        private CompletionQueue(uint* head, uint* tail, uint* ringMask, uint* ringEntries, uint* overflow, io_uring_cqe* cqes)
        {
            _head = head;
            _tail = tail;
            _ringMask = ringMask;
            _ringEntries = ringEntries;
            _overflow = overflow;
            _cqes = cqes;
            _headInternal = head;
            _tailInternal = tail;
        }

        public static CompletionQueue CreateCompletionQueue(void* ringBase, io_cqring_offsets* offsets) =>
            new CompletionQueue(
                head: Add<uint>(ringBase, offsets->head),
                tail: Add<uint>(ringBase, offsets->tail),
                ringMask: Add<uint>(ringBase, offsets->ring_mask),
                ringEntries: Add<uint>(ringBase, offsets->ring_entries),
                overflow: Add<uint>(ringBase, offsets->overflow),
                cqes: Add<io_uring_cqe>(ringBase, offsets->cqes)
            );

        public bool TryRead(int ringFd, bool kernelIoPolling, ref Completion result)
        {
            return TryRead(ringFd, kernelIoPolling, ref result, true);
        }

        public Completion Read(int ringFd, bool kernelIoPolling)
        {
            Completion completion = default;

            while (true)
            {
                int res = io_uring_enter(ringFd, 0, 1, IORING_ENTER_GETEVENTS, (sigset_t*) NULL);
                if (res < 0)
                {
                    ThrowErrnoException();
                }

                if (TryRead(ringFd, kernelIoPolling, ref completion, true))
                {
                    return completion;
                }
            }
        }

        public void Read(int ringFd, bool kernelIoPolling, Span<Completion> results)
        {
            int read = 0;
            while (read < results.Length)
            {
                // Head is moved below to avoid memory barrier in loop
                if (TryRead(read, kernelIoPolling, ref results[read], false))
                {
                    read++;
                    continue; // keep on reading without syscall-ing
                }

                int res = io_uring_enter(ringFd, 0, (uint) (results.Length - read), IORING_ENTER_GETEVENTS, (sigset_t*) NULL);
                if (res < 0)
                {
                    ThrowErrnoException();
                }
            }

            // Move head now, as we skipped the memory barrier in the TryRead above
            Volatile.Write(ref *_head, *_headInternal);
        }

        private bool TryRead(int ringFd, bool kernelIoPolling, ref Completion result, bool bumpHead)
        {
            uint head = *_head;

            // Try read from internal tail first to avoid memory barrier
            bool eventsAvailable = head != *_tailInternal;

            if (!eventsAvailable)
            {
                if (kernelIoPolling)
                {
                    // If the kernel is polling I/O, we must reap completions.
                    PollCompletion(ringFd);
                }

                // double-check with a memory barrier to ensure we see everything the kernel manipulated prior to the tail bump
                eventsAvailable = head != Volatile.Read(ref *_tail);
                _tailInternal = _tail;

                // piggy-back on the read-barrier above to verify that we have no overflows
                uint overflow = *_overflow;
                if (overflow > 0)
                {
                    ThrowOverflowException(overflow);
                }
            }

            if (!eventsAvailable)
            {
                return false;
            }

            var index = head & *_ringMask;
            var cqe = &_cqes[index];

            result = new Completion(cqe->res, cqe->user_data);

            *_headInternal = unchecked(*_headInternal + 1);
            if (bumpHead)
            {
                // ensure the kernel can take notice of us consuming the Events
                Volatile.Write(ref *_head, *_headInternal);
            }

            return true;
        }

        private static void PollCompletion(int ringFd)
        {
            // We are not expected to block if no completions are available, so min_complete is set to 0.
            int ret = io_uring_enter(ringFd, 0, 0, IORING_ENTER_GETEVENTS, (sigset_t*) NULL);
            if (ret < 0)
            {
                ThrowErrnoException();
            }
        }
    }
}
