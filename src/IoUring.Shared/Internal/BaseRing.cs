using System;
using System.Runtime.InteropServices;
using Tmds.Linux;
using static Tmds.Linux.LibC;
using static IoUring.Internal.Helpers;
using static IoUring.Internal.ThrowHelper;

namespace IoUring.Internal
{
    public abstract unsafe partial class BaseRing : IDisposable
    {
        private readonly uint _flags;
        private readonly uint _features;
        private protected readonly CloseHandle _ringFd;

        private protected readonly SubmissionQueue _sq;
        private readonly UnmapHandle _sqHandle;
        private readonly UnmapHandle _sqeHandle;

        private protected readonly CompletionQueue _cq;
        private readonly UnmapHandle _cqHandle;

        private static int Setup(uint entries, io_uring_params* p, RingOptions? options)
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
            size_t sqSize = SqSize(p);
            size_t cqSize = CqSize(p);

            if ((p->features & IORING_FEAT_SINGLE_MMAP) != 0)
            {
                sqSize = cqSize = (size_t) Math.Max(cqSize, sqSize);
            }

            return (sqSize, cqSize);
        }

        private static SubmissionQueue MapSq(int ringFd, size_t sqSize, io_uring_params* p, bool sqPolled, bool ioPolled, out UnmapHandle sqHandle, out UnmapHandle sqeHandle)
        {
            var ptr = mmap(NULL, sqSize, PROT_READ | PROT_WRITE, MAP_SHARED | MAP_POPULATE, ringFd, (long) IORING_OFF_SQ_RING);
            if (ptr == MAP_FAILED)
            {
                ThrowErrnoException();
            }
            sqHandle = new UnmapHandle(ptr, sqSize);

            size_t sqeSize = SqeSize(p);
            var sqePtr = mmap(NULL, sqeSize, PROT_READ | PROT_WRITE, MAP_SHARED | MAP_POPULATE, ringFd, (long) IORING_OFF_SQES);
            if (sqePtr == MAP_FAILED)
            {
                ThrowErrnoException();
            }
            sqeHandle = new UnmapHandle(sqePtr, sqeSize);

            return new SubmissionQueue(ptr, &p->sq_off, (io_uring_sqe*) sqePtr, sqPolled, ioPolled);
        }

        private static CompletionQueue MapCq(int ringFd, size_t cqSize, io_uring_params* p, UnmapHandle sqHandle, bool ioPolled, out UnmapHandle cqHandle)
        {
            void* ptr;

            if ((p->features & IORING_FEAT_SINGLE_MMAP) != 0)
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

            return new CompletionQueue(ptr, &p->cq_off, ioPolled);
        }

        protected BaseRing(int entries, RingOptions? ringOptions = default)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) ThrowPlatformNotSupportedException();
            if (entries < 1) ThrowArgumentOutOfRangeException(ExceptionArgument.entries);

            io_uring_params p = default;
            int fd = Setup((uint) entries, &p, ringOptions);

            _ringFd = new CloseHandle();
            _ringFd.SetHandle(fd);

            _flags = p.flags;
            _features = p.features;

            var (sqSize, cqSize) = GetSize(&p);

            try
            {
                _sq = MapSq(fd, sqSize, &p, SubmissionPollingEnabled, IoPollingEnabled, out _sqHandle, out _sqeHandle);
                _cq = MapCq(fd, cqSize, &p, _sqHandle, IoPollingEnabled, out _cqHandle);
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

        /// <summary>
        /// Whether protection against Completion Queue overflow is supported by the kernel.
        /// </summary>
        public bool SupportsNoDrop => (_features & IORING_FEAT_NODROP) != 0;

        /// <summary>
        /// Whether the application can be certain, that any data needed for async offload has been consumed by the
        /// kernel, when the Submission Queue Entry is consumed.
        /// </summary>
        public bool SupportsStableSubmits => (_features & IORING_FEAT_SUBMIT_STABLE) != 0;

        /// <summary>
        /// Returns the maximum number of events the Submission Queue can contain
        /// </summary>
        public int SubmissionQueueSize => (int) _sq.Entries;

        /// <summary>
        /// Returns the maximum number of events the Completion Queue can contain
        /// </summary>
        public int CompletionQueueSize => (int) _cq.Entries;

        /// <summary>
        /// Returns the number of un-submitted entries in the Submission Queue
        /// </summary>
        public int SubmissionEntriesUsed => (int) _sq.EntriesToSubmit;

        /// <summary>
        /// Returns the number of free entries in the Submission Queue
        /// </summary>
        public int SubmissionEntriesAvailable => (int) _sq.EntriesToPrepare;

        /// <summary>
        /// Notifies the kernel of the availability of new Submission Queue Entries and waits for a given number of completions to occur.
        /// This typically requires a syscall and should be deferred as long as possible.
        /// </summary>
        /// <param name="minComplete">The number of completed Submission Queue Entries required before returning</param>
        /// <param name="operationsSubmitted">(out) The number of submitted Submission Queue Entries</param>
        /// <returns>The result of the operation</returns>
        /// <exception cref="ErrnoException">On negative result from syscall with errno other than EAGAIN, EBUSY and EINTR</exception>
        public SubmitResult SubmitAndWait(uint minComplete, out uint operationsSubmitted)
            => _sq.SubmitAndWait(_ringFd.DangerousGetHandle().ToInt32(), minComplete, out operationsSubmitted);

        /// <summary>
        /// Notifies the kernel of the availability of new Submission Queue Entries.
        /// This typically requires a syscall and should be deferred as long as possible.
        /// </summary>
        /// <param name="operationsSubmitted">(out) The number of submitted Submission Queue Entries</param>
        /// <returns>The result of the operation</returns>
        /// <exception cref="ErrnoException">On negative result from syscall with errno other than EAGAIN, EBUSY and EINTR</exception>
        public SubmitResult Submit(out uint operationsSubmitted)
            => SubmitAndWait(0, out operationsSubmitted);

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose()
        {
            _ringFd.Dispose();
            _sqHandle?.Dispose();
            _sqeHandle.Dispose();
            if (_sqHandle != _cqHandle)
                _cqHandle?.Dispose();
        }
    }
}