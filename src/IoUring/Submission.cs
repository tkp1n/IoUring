using Tmds.Linux;

namespace IoUring
{
    public readonly unsafe partial struct Submission
    {
        private readonly io_uring_sqe* _sqe;

        internal Submission(io_uring_sqe* sqe)
        {
            _sqe = sqe;
        }
    }
}