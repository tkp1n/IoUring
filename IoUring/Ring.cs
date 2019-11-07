using System;
using Tmds.Linux;
using IoUring.Internal;
using static Tmds.Linux.LibC;
using static IoUring.Internal.Helpers;
using static IoUring.Internal.ThrowHelper;

namespace IoUring
{
    public sealed unsafe partial class Ring : IDisposable
    {
        private readonly uint _flags;
        private readonly CloseHandle _ringFd;

        private static int Setup(uint entries, io_uring_params* p, RingOptions options)
        {
            options?.WriteTo(p);

            int fd = io_uring_setup(entries, p);
            if (fd < 0)
            {
                ThrowErrnoException();
            }

            return fd;
        }

        private static size_t SqSize(io_uring_params* p) 
            =>  p->sq_off.array + p->sq_entries * sizeof(uint);

        private static size_t SqeSize(io_uring_params* p)
            => (size_t) (p->sq_entries * (ulong) sizeof(io_uring_sqe));

        private static size_t CqSize(io_uring_params* p) 
            => (size_t)(p->cq_off.cqes + p->cq_entries * (ulong)sizeof(io_uring_cqe));

        private static (size_t sqSize, size_t cqSize) GetSize(io_uring_params* p)
        {
            var sqSize = SqSize(p);
            var cqSize = CqSize(p);

            if ((p->resv[0] & IORING_FEAT_SINGLE_MMAP) != 0) // TODO: Use p->features, once in Tmds.Linux
            {
                sqSize = cqSize = (size_t) Math.Max(cqSize, sqSize);
            }

            return (sqSize, cqSize);
        }

        private static SubmissionQueue MapSq(int ringFd, size_t sqSize, io_uring_params* p, out UnmapHandle sqHandle, out UnmapHandle sqeHandle)
        {
            void* ptr = mmap(NULL, sqSize, PROT_READ | PROT_WRITE, MAP_SHARED | MAP_POPULATE, ringFd, (long) IORING_OFF_SQ_RING);
            if (ptr == MAP_FAILED)
            {
                ThrowErrnoException();
            }
            sqHandle = new UnmapHandle(ptr, sqSize);

            size_t sqeSize = SqeSize(p);
            void* sqePtr = mmap(NULL, sqeSize, PROT_READ | PROT_WRITE, MAP_SHARED | MAP_POPULATE, ringFd, (long) IORING_OFF_SQES);
            if (sqePtr == MAP_FAILED)
            {
                ThrowErrnoException();
            }
            sqeHandle = new UnmapHandle(sqePtr, sqeSize);
            
            return SubmissionQueue.CreateSubmissionQueue(ptr, &p->sq_off, (io_uring_sqe*) sqePtr);
        }

        private static CompletionQueue MapCq(int ringFd, size_t cqSize, io_uring_params* p, UnmapHandle sqHandle, out UnmapHandle cqHandle)
        {
            void* ptr;

            if ((p->resv[0] & IORING_FEAT_SINGLE_MMAP) != 0) // TODO: Use p->features, once in Tmds.Linux
            {
                ptr = sqHandle.DangerousGetHandle().ToPointer();
                cqHandle = sqHandle;
            }
            else
            {
                ptr = mmap(NULL, cqSize, PROT_READ | PROT_WRITE, MAP_SHARED | MAP_POPULATE, ringFd, (long) IORING_OFF_CQ_RING);
                if (ptr == MAP_FAILED)
                {
                    ThrowErrnoException();
                }

                cqHandle = new UnmapHandle(ptr, cqSize);
            }

            return CompletionQueue.CreateCompletionQueue(ptr, &p->cq_off);
        }

        public Ring(int entries, RingOptions ringOptions = default)
        {
            if (entries < 1) throw new ArgumentOutOfRangeException(nameof(entries), "must be between 1..4096 (both inclusive)");
            if (entries > 4096) throw new ArgumentOutOfRangeException(nameof(entries), "must be between 1..4096 (both inclusive)");
            VerifyPowerOfTwo(entries, nameof(entries), "must be a power of two");

            io_uring_params p = default;
            int fd = Setup((uint) entries, &p, ringOptions);

            _ringFd = new CloseHandle();
            _ringFd.SetHandle(fd);

            _flags = p.flags;

            var (sqSize, cqSize) = GetSize(&p);

            try
            {
                _sq = MapSq(fd, sqSize, &p, out _sqHandle, out _sqeHandle);
                _cq = MapCq(fd, cqSize, &p, _sqHandle, out _cqHandle);
            }
            catch (ErrnoException)
            {
                // Ensure we don't leak file handles on error
                Dispose();
                throw;
            }
        }

        /// <summary>
        /// Whether the kernel is polling for entries on the Submission Queue.
        /// </summary>
        public bool SubmissionPollingEnabled => (_flags & IORING_SETUP_SQPOLL) != 0;

        /// <summary>
        /// Whether the kernel Submission Queue polling thread is created with CPU affinity.
        /// </summary>
        public bool SubmissionQueuePollingCpuAffinity => (_flags & IORING_SETUP_SQ_AFF) != 0;

        /// <summary>
        /// Whether the kernel to polls for I/O completions (instead of using interrupt driven I/O).
        /// </summary>
        public bool IoPollingEnabled => (_flags & IORING_SETUP_IOPOLL) != 0;

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose()
        {
            _ringFd?.Dispose();
            _sqHandle?.Dispose();
            _sqeHandle?.Dispose();
            if (_sqHandle != _cqHandle)
                _cqHandle?.Dispose();
        }
    }
}
