using Tmds.Linux;
using static Tmds.Linux.LibC;
using static IoUring.Internal.Helpers;

namespace IoUring.Internal
{
    internal sealed unsafe partial class CompletionQueue
    {
        /// <summary>
        /// File descriptor of the io_uring instance.
        /// </summary>
        private readonly int _ringFd;

        /// <summary>
        /// Incremented by the application to let the kernel know, which Completion Queue Events were already consumed.
        /// </summary>
        private readonly uint* _head;

        /// <summary>
        /// Incremented by the kernel to let the application know about another Completion Queue Event.
        /// </summary>
        private readonly uint* _tail;

        /// <summary>
        /// Mask to apply to potentially overflowing head counter to get a valid index within the ring.
        /// </summary>
        private readonly uint _ringMask;

        /// <summary>
        /// Number of entries in the ring.
        /// </summary>
        private readonly uint _ringEntries;

        /// <summary>
        /// Incremented by the kernel on each overwritten Completion Queue Event.
        /// This is a sign, that the application is producing Submission Queue Events faster as it handles the corresponding Completion Queue Events.
        /// </summary>
        private readonly uint* _overflow;

        /// <summary>
        /// Completion Queue Events filled by the kernel.
        /// </summary>
        private readonly io_uring_cqe* _cqes;

        /// <summary>
        /// Incremented by the application to indicate which Completion Queue Events were already consumed.
        /// This is typically ahead of <see cref="_head"/> as the kernel must not yet know about bumps to this internal index to avoid write-barriers.
        /// </summary>
        private readonly uint* _headInternal;

        /// <summary>
        /// Internal index to the most recent Completion Queue Event known to the application.
        /// This is typically behind <see cref="_tail"/> as the internal index is only updated (read-barrier) when necessary.
        /// </summary>
        private uint* _tailInternal;

        /// <summary>
        /// Whether the kernel polls for I/O
        /// </summary>
        private readonly bool _ioPolled;

        public CompletionQueue(int ringFd, void* ringBase, io_cqring_offsets* offsets, bool ioPolled)
        {
            _ringFd = ringFd;
            _head = Add<uint>(ringBase, offsets->head);
            _tail = Add<uint>(ringBase, offsets->tail);
            _ringMask = *Add<uint>(ringBase, offsets->ring_mask);
            _ringEntries = *Add<uint>(ringBase, offsets->ring_entries);
            _overflow = Add<uint>(ringBase, offsets->overflow);
            _cqes = Add<io_uring_cqe>(ringBase, offsets->cqes);
            _ioPolled = ioPolled;
            _headInternal = _head;
            _tailInternal = _tail;
        }

        /// <summary>
        /// Returns the number of entries in the Completion Queue.
        /// </summary>
        public uint Entries => _ringEntries;

        private void PollCompletion()
        {
            // We are not expected to block if no completions are available, so min_complete is set to 0.
            SafeEnter(_ringFd, 0, 0, IORING_ENTER_GETEVENTS);
        }
    }
}