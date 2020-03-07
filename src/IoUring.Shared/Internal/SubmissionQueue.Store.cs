using Tmds.Linux;
using static IoUring.Internal.Helpers;

namespace IoUring.Internal
{
    internal sealed unsafe partial class SubmissionQueue
    {
        /// <summary>
        /// Incremented by the kernel to let the application know, another element was consumed.
        /// </summary>
        private readonly uint* _head;

        /// <summary>
        /// Incremented by the application to let the kernel know, another element was submitted.
        /// </summary>
        private readonly uint* _tail;

        /// <summary>
        /// Mask to apply to potentially overflowing tail counter to get a valid index within the ring
        /// </summary>
        private readonly uint _ringMask;

        /// <summary>
        /// Number of entries in the ring
        /// </summary>
        private readonly uint _ringEntries;

        /// <summary>
        /// Set to IORING_SQ_NEED_WAKEUP by the kernel, if the Submission Queue polling thread is idle and needs
        /// a call to io_uring_enter with the IORING_ENTER_SQ_WAKEUP flag set.
        /// </summary>
        private readonly uint* _flags;

        /// <summary>
        /// Incremented by the kernel on each invalid submission.
        /// </summary>
        private readonly uint* _dropped;

        /// <summary>
        /// Array of indices within the <see cref="_sqes"/>
        /// </summary>
        private readonly uint* _array;

        /// <summary>
        /// Submission Queue Entries to be filled by the application
        /// </summary>
        private readonly io_uring_sqe* _sqes;

        /// <summary>
        /// Index of the last Submission Queue Entry handed out to the application (to be filled).
        /// This is typically behind <see cref="_tail"/> as the kernel must not yet know about bumps of the internal index, before the Entry is fully prepped.
        /// </summary>
        private uint _tailInternal;

        /// <summary>
        /// Index of the last Submission Queue Entry handed over to the kernel.
        /// This is typically ahead of <see cref="_head"/> as the kernel might not have had the chance to consume the item at the given index.
        /// </summary>
        private uint _headInternal;

        /// <summary>
        /// Whether the kernel is polling the Submission Queue.
        /// </summary>
        private readonly bool _sqPolled;

        /// <summary>
        /// Whether the kernel is polling I/O
        /// </summary>
        private readonly bool _ioPolled;

        public SubmissionQueue(void* ringBase, io_sqring_offsets* offsets, io_uring_sqe* elements, bool sqPolled, bool ioPolled)
            : this(ringBase, offsets)
        {
            _head = Add<uint>(ringBase, offsets->head);
            _tail = Add<uint>(ringBase, offsets->tail);
            _ringMask = *Add<uint>(ringBase, offsets->ring_mask);
            _ringEntries = *Add<uint>(ringBase, offsets->ring_entries);
            _flags = Add<uint>(ringBase, offsets->flags);
            _dropped = Add<uint>(ringBase, offsets->dropped);
            _array = Add<uint>(ringBase, offsets->array);
            _sqes = elements;
            _tailInternal = 0;
            _headInternal = 0;
            _sqPolled = sqPolled;
            _ioPolled = ioPolled;
        }
    }
}