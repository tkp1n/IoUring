using Tmds.Linux;
using static IoUring.Internal.Helpers;

namespace IoUring
{
    public unsafe partial class Ring
    {
        public bool TryGetSubmissionQueueEntryUnsafe(out Submission submission)
        {
            if (!NextSubmissionQueueEntry(out var sqe))
            {
                submission = default;
                return false;
            }

            submission = new Submission(sqe);
            return true;
        }

        // Visible for testing
        internal bool TryPrepareReadWrite(byte op, int fd, void* iov, int count, off_t offset, int flags, ulong userData, SubmissionOption options)
        {
            if (!NextSubmissionQueueEntry(out var sqe))
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

        private bool NextSubmissionQueueEntry(out io_uring_sqe* sqe)
            => (sqe = _sq.NextSubmissionQueueEntry()) != NULL;
    }
}
