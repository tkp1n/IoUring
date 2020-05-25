using System.Runtime.CompilerServices;
using IoUring.Internal;
using Tmds.Linux;
using static IoUring.Internal.Helpers;

namespace IoUring
{
    public unsafe partial class Ring
    {
        /// <summary>
        /// If possible, returns a <see cref="Submission"/> to be prepared by the caller prior to any call to <see cref="SubmitAndWait"/>.
        /// The <see cref="Submission"/> must not be used after a call to <see cref="SubmitAndWait"/>. No safe-guards enforce this.
        /// </summary>
        /// <param name="submission">Submission to be prepared manually.</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryGetSubmissionQueueEntryUnsafe(out Submission submission)
        {
            if (NextSubmissionQueueEntry(out var sqe) != SubmissionAcquireResult.SubmissionAcquired)
            {
                submission = default;
                return false;
            }

            submission = new Submission(sqe);
            return true;
        }

        /// <summary>
        /// Notifies the kernel of the availability of new Submission Queue Entries and waits for a given number of completions to occur.
        /// This typically requires a syscall and should be deferred as long as possible.
        /// </summary>
        /// <param name="minComplete">The number of completed Submission Queue Entries required before returning</param>
        /// <param name="skip">Number of first Submission Queue Entries to skip</param>
        /// <param name="operationsSubmitted">(out) The number of submitted Submission Queue Entries</param>
        /// <returns>The result of the operation</returns>
        /// <exception cref="ErrnoException">On negative result from syscall with errno other than EAGAIN, EBUSY and EINTR</exception>
        public SubmitResult SubmitAndWait(uint minComplete, uint skip, out uint operationsSubmitted)
        {
            _operationsInFlight -= skip; // adjust operations in flight as skipped operations are never really flying
            return _sq.SubmitAndWait(minComplete, skip, out operationsSubmitted);
        }

        // Visible for testing
        internal bool TryPrepareReadWrite(byte op, int fd, void* iov, int count, off_t offset, int flags, ulong userData, SubmissionOption options)
        {
            if (NextSubmissionQueueEntry(out var sqe) != SubmissionAcquireResult.SubmissionAcquired)
                return false;

            unchecked
            {
                sqe->opcode = op;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->off = (ulong) (long) offset;
                sqe->addr = (ulong) iov;
                sqe->len = (uint) count;
                sqe->rw_flags = flags;
                sqe->user_data = userData;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private SubmissionAcquireResult NextSubmissionQueueEntry(out io_uring_sqe* sqe)
        {
            sqe = (io_uring_sqe*) NULL;

            if (!CheckAndIncrementOperationsInFlight())
                return SubmissionAcquireResult.TooManyOperationsInFlight;

            var result = _sq.NextSubmissionQueueEntry();
            if (result == NULL) return SubmissionAcquireResult.SubmissionQueueFull;

            sqe = result;
            return SubmissionAcquireResult.SubmissionAcquired;
        }
    }
}
