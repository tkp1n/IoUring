using System.Diagnostics;
using System.Threading;
using Tmds.Linux;
using static IoUring.Internal.Helpers;

namespace IoUring.Internal
{
    internal unsafe class CompletionQueue
    {
        /// <summary>
        /// Incremented by the application to let the kernel know, which Completion Queue Events were already consumed.
        /// </summary>
        public uint* head;

        /// <summary>
        /// Incremented by the kernel to let the application know about another Completion Queue Event. 
        /// </summary>
        public uint* tail;

        /// <summary>
        /// Mask to apply to potentially overflowing head counter to get a valid index within the ring.
        /// </summary>
        public uint* ringMask;

        /// <summary>
        /// Number of entries in the ring.
        /// </summary>
        public uint* ringEntries;

        /// <summary>
        /// Incremented by the kernel on each overwritten Completion Queue Event.
        /// This is a sign, that the application is producing Submission Queue Events faster as it handles the corresponding Completion Queue Events. 
        /// </summary>
        public uint* overflow;

        /// <summary>
        /// Completion Queue Events filled by the kernel.
        /// </summary>
        public io_uring_cqe* cqes;

        private CompletionQueue(uint* head, uint* tail, uint* ringMask, uint* ringEntries, uint* overflow, io_uring_cqe* cqes)
        {
            this.head = head;
            this.tail = tail;
            this.ringMask = ringMask;
            this.ringEntries = ringEntries;
            this.overflow = overflow;
            this.cqes = cqes;
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
        public void AssertNoOverflows() => Debug.Assert(Volatile.Read(ref *overflow) == 0);
    }
}
