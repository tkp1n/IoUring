using System;

namespace IoUring.Concurrent
{
    [Flags]
    public enum ConcurrentSubmissionOption : byte
    {
        /// <summary>
        /// No option selected.
        /// </summary>
        None = 0x00,

        /// <summary>
        /// When this flag is specified, fd is an index into the files array registered with
        /// the <see cref="ConcurrentRing"/> instance. <seealso cref="ConcurrentRing.RegisterFiles"/>
        /// </summary>
        /// <remarks>Available since 5.1</remarks>
        FixedFile = 1 << 0, // IOSQE_FIXED_FILE

        /// <summary>
        /// When this flag is specified, the SQE will not be started before previously
        /// submitted SQEs have completed, and new SQEs will not be started before this
        /// one completes.
        /// </summary>
        /// <remarks>Available since 5.2.</remarks>
        Drain = 1 << 1, // IOSQE_IO_DRAIN

        /// <summary>
        /// Normal operation for io_uring is to try and issue an sqe as non-blocking first,
        /// and if that fails, execute it in an async manner. To support more efficient
        /// overlapped operation of requests that the application knows/assumes will
        /// always (or most of the time) block, the application can ask for an sqe to be
        /// issued async from the start.
        /// </summary>
        /// <remarks>Available since 5.6</remarks>
        Async = 1 << 4, // IOSQE_IO_ASYNC
    }
}