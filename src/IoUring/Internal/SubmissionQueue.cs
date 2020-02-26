using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Tmds.Linux;
using static Tmds.Linux.LibC;
using static IoUring.Internal.Helpers;
using static IoUring.Internal.ThrowHelper;

namespace IoUring.Internal
{
    internal unsafe class SubmissionQueue
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

        private SubmissionQueue(uint* head, uint* tail, uint ringMask, uint ringEntries, uint* flags, uint* dropped, uint* array, io_uring_sqe* sqes, bool sqPolled)
        {
            _head = head;
            _tail = tail;
            _ringMask = ringMask;
            _ringEntries = ringEntries;
            _flags = flags;
            _dropped = dropped;
            _array = array;
            _sqes = sqes;
            _tailInternal = 0;
            _headInternal = 0;
            _sqPolled = sqPolled;
        }

        public static SubmissionQueue CreateSubmissionQueue(void* ringBase, io_sqring_offsets* offsets, io_uring_sqe* elements, bool sqPolled)
            => new SubmissionQueue(
                head: Add<uint>(ringBase, offsets->head),
                tail: Add<uint>(ringBase, offsets->tail),
                ringMask: *Add<uint>(ringBase, offsets->ring_mask),
                ringEntries: *Add<uint>(ringBase, offsets->ring_entries),
                flags: Add<uint>(ringBase, offsets->flags),
                dropped: Add<uint>(ringBase, offsets->dropped),
                array: Add<uint>(ringBase, offsets->array),
                sqes: elements,
                sqPolled: sqPolled
            );

        /// <summary>
        /// Returns the number of entries in the Submission Queue.
        /// </summary>
        public uint Entries => _ringEntries;

        /// <summary>
        /// Determines the number of entries in the Submission Queue that can be used to prepare new submissions
        /// prior to the next <see cref="SubmitAndWait"/>.
        /// </summary>
        public uint EntriesToPrepare
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _ringEntries - EntriesToSubmit;
        }

        /// <summary>
        /// Calculates the number of prepared Submission Queue Entries that will be submitted to the kernel during
        /// the next <see cref="SubmitAndWait"/>.
        /// </summary>
        public uint EntriesToSubmit
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                uint head = _sqPolled ? Volatile.Read(ref *_head) : _headInternal;
                return unchecked(_tailInternal - head);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private io_uring_sqe* InternalNextSqe(uint head)
        {
            uint tailInternal = _tailInternal;
            uint next = unchecked(tailInternal + 1);

            if (next - head > _ringEntries)
            {
                return (io_uring_sqe*) NULL;
            }

            var sqe = &_sqes[tailInternal & _ringMask];
            _tailInternal = next;

            // Handout cleaned sqe
            Unsafe.InitBlockUnaligned(sqe, 0x00, (uint) sizeof(io_uring_sqe));
            return sqe;
        }

        /// <summary>
        /// Finds the next Submission Queue Entry to be written to. The entry will be initialized with zeroes.
        /// If the Submission Queue is full, a null-pointer is returned.
        /// </summary>
        /// <returns>The next Submission Queue Entry to be written to or null if the Queue is full</returns>
        public io_uring_sqe* NextSubmissionQueueEntry()
        {
            if (_sqPolled)
            {
                return InternalNextSqe(Volatile.Read(ref *_head));
            }

            return InternalNextSqe(_headInternal);
        }

        /// <summary>
        /// Make prepared Submission Queue Entries visible to the kernel.
        /// </summary>
        /// <returns>
        /// The number Submission Queue Entries that can be submitted.
        /// This may include Submission Queue Entries previously ignored by the kernel.</returns>
        private uint Notify()
        {
            uint tail = *_tail;
            uint tailInternal = _tailInternal;
            uint headInternal = _headInternal;
            if (headInternal == tailInternal)
            {
                return tail - *_head;
            }

            uint mask = _ringMask;
            uint* array = _array;
            uint toSubmit = unchecked(tailInternal - headInternal);
            while (toSubmit-- != 0)
            {
                array[tail & mask] = headInternal & mask;
                tail = unchecked(tail + 1);
                headInternal = unchecked(headInternal + 1);
            }

            _headInternal = headInternal;

            // write barrier to ensure all manipulations above are visible to the kernel once the tail-bump is observed
            Volatile.Write(ref *_tail, tail);

            return tail - *_head;
        }

        private bool ShouldEnter(out uint enterFlags)
        {
            enterFlags = 0;
            if (!_sqPolled) return true;
            if ((*_flags & IORING_SQ_NEED_WAKEUP) != 0)
            {
                // Kernel is polling but transitioned to idle (IORING_SQ_NEED_WAKEUP)
                enterFlags |= IORING_ENTER_SQ_WAKEUP;
                return true;
            }

            // Kernel is still actively polling
            return false;
        }

        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        private void CheckNoSubmissionsDropped() => Debug.Assert(Volatile.Read(ref *_dropped) == 0);

        public SubmitResult SubmitAndWait(int ringFd, uint minComplete, out uint operationsSubmitted)
        {
            uint toSubmit = Notify();

            if (!ShouldEnter(out uint enterFlags))
            {
                CheckNoSubmissionsDropped();

                // Assume all Entries are already known to the kernel via Notify above
                operationsSubmitted = toSubmit;
                return SubmitResult.SubmittedSuccessfully;
            }

            if (minComplete > 0) enterFlags |= IORING_ENTER_GETEVENTS; // required for minComplete to take effect

            int res;
            int err = default;
            do
            {
                res = io_uring_enter(ringFd, toSubmit, minComplete, enterFlags, (sigset_t*) NULL);
            } while (res == -1 && (err = errno) == EINTR);

            if (res == -1)
            {
                if (err == EAGAIN || err == EBUSY)
                {
                    operationsSubmitted = default;
                    return SubmitResult.AwaitCompletions;
                }

                ThrowErrnoException(res);
            }

            CheckNoSubmissionsDropped();

            return (operationsSubmitted = (uint) res) >= toSubmit ? 
                SubmitResult.SubmittedSuccessfully : 
                SubmitResult.SubmittedPartially;
        }
    }
}
