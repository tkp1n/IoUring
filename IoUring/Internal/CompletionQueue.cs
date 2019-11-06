using System;
using System.Diagnostics;
using System.Threading;
using Tmds.Linux;
using static Tmds.Linux.LibC;
using static IoUring.Internal.Helpers;

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

        private CompletionQueue(uint* head, uint* tail, uint* ringMask, uint* ringEntries, uint* overflow, io_uring_cqe* cqes)
        {
            _head = head;
            _tail = tail;
            _ringMask = ringMask;
            _ringEntries = ringEntries;
            _overflow = overflow;
            _cqes = cqes;
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

        [Conditional("DEBUG")]
        public void AssertNoOverflows() => Debug.Assert(Volatile.Read(ref *_overflow) == 0);

        public bool TryRead(ref Completion result)
        {
            // ensure we see everything the kernel manipulated prior to the head bump
            var head = Volatile.Read(ref *_head); 

            AssertNoOverflows();
            
            if (head == *_tail)
            {
                // No Completion Queue Event available
                return false;
            }

            var index = head & *_ringMask;
            var cqe = &_cqes[index];

            result = new Completion(cqe->res, cqe->user_data);

            // ensure the kernel can take notice of us reading the latest Event
            Volatile.Write(ref *_head, unchecked(head + 1)); 
            return true;
        }

        public int Read(int ringFd, Span<Completion> results)
        {
            int read = 0;
            while (read < results.Length)
            {
                if (TryRead(ref results[read]))
                {
                    read++;
                    continue; // keep on reading without syscall-ing
                }

                int res = io_uring_enter(ringFd, 0, (uint) (results.Length - read), IORING_ENTER_GETEVENTS, (sigset_t*) NULL);
                if (res < 0)
                {
                    throw new ErrnoException(errno);
                }
            }

            return read;
        }
    }
}
