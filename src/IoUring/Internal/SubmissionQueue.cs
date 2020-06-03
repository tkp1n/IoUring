using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Tmds.Linux;
using static Tmds.Linux.LibC;
using static IoUring.Internal.Helpers;
using static IoUring.Internal.ThrowHelper;

namespace IoUring.Internal
{
    internal sealed unsafe partial class SubmissionQueue
    {
        private SubmissionQueue(void* ringBase, io_sqring_offsets* offsets) { }

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
        /// <param name="skip">Number of first Submission Queue Entries to skip</param>
        /// <returns>
        /// The number Submission Queue Entries that can be submitted.
        /// This may include Submission Queue Entries previously ignored by the kernel.</returns>
        private uint Notify(uint skip)
        {
            uint tail = *_tail;
            uint head = *_head;
            uint pending = unchecked(tail - head);
            uint tailInternal = _tailInternal;
            uint headInternal = _headInternal + skip;
            if (headInternal == tailInternal)
            {
                return pending;
            }

            uint mask = _ringMask;
            uint* array = _array;
            uint toSubmit = unchecked(tailInternal - headInternal);
            tail = unchecked(tail - pending);

            while (toSubmit-- != 0)
            {
                array[tail & mask] = headInternal & mask;
                tail = unchecked(tail + 1);
                headInternal = unchecked(headInternal + 1);
            }

            // write barrier to ensure all manipulations above are visible to the kernel once the tail-bump is observed
            Volatile.Write(ref *_tail, tail);

            return tail - head;
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

        public SubmitResult SubmitAndWait(uint minComplete, out uint operationsSubmitted)
            => SubmitAndWait(minComplete, 0, out operationsSubmitted);

        public SubmitResult SubmitAndWait(uint minComplete, uint skip, out uint operationsSubmitted)
        {
            uint toSubmit = Notify(skip);

            if (!ShouldEnter(out uint enterFlags))
            {
                CheckNoSubmissionsDropped();

                // Assume all Entries are already known to the kernel via Notify above
                goto SkipSyscall;
            }

            // For minComplete to take effect or if the kernel is polling for I/O, we must set IORING_ENTER_GETEVENTS
            if (minComplete > 0 || _ioPolled)
            {
                enterFlags |= IORING_ENTER_GETEVENTS; // required for minComplete to take effect
            }
            else if (toSubmit == 0)
            {
                // There are no submissions, we don't have to wait for completions and don't have to reap polled I/O completions
                // --> We can skip the syscall and return directly.
                goto SkipSyscall;
            }

            int ringFd = _ringFd;
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

            _headInternal = unchecked(_headInternal + skip + (uint)res);

            return (operationsSubmitted = (uint) res) >= toSubmit ?
                SubmitResult.SubmittedSuccessfully :
                SubmitResult.SubmittedPartially;

        SkipSyscall:
            _headInternal = unchecked(_headInternal + skip);
            operationsSubmitted = toSubmit;
            return SubmitResult.SubmittedSuccessfully;
        }
    }
}
