using Tmds.Linux;

namespace IoUring.Concurrent
{
    public readonly unsafe partial struct Submission
    {
        private readonly io_uring_sqe* _sqe;

        internal Submission(io_uring_sqe* sqe, uint index)
        {
            _sqe = sqe;
            Index = index;
        }

        internal uint Index { get; }

        // internal for testing
        internal void PrepareReadWrite(byte op, int fd, void* iov, int count, off_t offset, int flags, ulong userData, SubmissionOption options)
        {
            var sqe = _sqe;

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
        }
    }
}