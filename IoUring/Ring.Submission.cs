using System;
using Tmds.Linux;
using IoUring.Internal;
using static Tmds.Linux.LibC;
using static IoUring.Internal.Helpers;
using static IoUring.Internal.ThrowHelper;

namespace IoUring 
{
    public unsafe partial class Ring
    {
        private readonly SubmissionQueue _sq;
        private readonly UnmapHandle _sqHandle;
        private readonly UnmapHandle _sqeHandle;

        /// <summary>
        /// Adds a NOP to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options"></param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareNop(ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            if (!TryPrepareNop(userData, options))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add a NOP to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options"></param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareNop(ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            io_uring_sqe* sqe = _sq.NextSubmissionQueueEntry();
            if (sqe == NULL) return false;

            sqe->opcode = IORING_OP_NOP;
            sqe->user_data = userData;
            sqe->flags |= (byte) options;

            return true;
        }

        /// <summary>
        /// Submits all pending (not previously submitted) Submission Queue Entries to the kernel.
        /// Submitting an Entry makes it available to the kernel if polling is enabled. Otherwise, the submission
        /// will be ignored until flushed.
        /// It is profitable to submit as many Entries as possible before performing the Flush, as the Flush operation includes a syscall.
        /// </summary>
        /// <returns>The number of submitted Entries</returns>
        public uint Submit() => _sq.Submit();

        /// <summary>
        /// Notifies the kernel of the availability of new Submission Queue Entries.
        /// This typically requires a syscall and should be deferred as long as possible. 
        /// </summary>
        /// <param name="toFlush">Number of un-flushed Submission Queue Entries</param>
        /// <param name="minComplete">The number of completed Submission Queue Entries required before returning (default = 0)</param>
        /// <returns>The number of flushed Submission Queue Entries</returns>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="minComplete"/> > <paramref name="toFlush"/></exception>
        /// <exception cref="SubmissionEntryDroppedException">If an invalid Submission Queue Entry was dropped</exception>
        /// <exception cref="ErrnoException">On negative result from syscall</exception>
        public uint Flush(uint toFlush, uint minComplete = 0)
            => _sq.Flush(_ringFd.DangerousGetHandle().ToInt32(), SubmissionPollingEnabled, toFlush, minComplete);
    }
}
