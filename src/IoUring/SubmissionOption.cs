using System;

namespace IoUring
{
    [Flags]
    public enum SubmissionOption : byte
    {
        /// <summary>
        /// No option selected.
        /// </summary>
        None = 0x00,

        /// <summary>
        /// When this flag is specified, the SQE will not be started before previously
        /// submitted SQEs have completed, and new SQEs will not be started before this
        /// one completes.
        /// </summary>
        /// <remarks>Available since 5.2.</remarks>
        Drain = 1 << 1, // IOSQE_IO_DRAIN

        /// <summary>
        /// When this flag is specified, it forms a link with the next SQE in the
        /// submission ring. That next SQE will not be started before this one completes.
        /// This, in effect, forms a chain of SQEs, which can be arbitrarily long. The tail
        /// of the chain is denoted by the first SQE that does not have this flag set.
        /// This flag has no effect on previous SQE submissions, nor does it impact SQEs
        /// that are outside of the chain tail. This means that multiple chains can be
        /// executing in parallel, or chains and individual SQEs. Only members inside the
        /// chain are serialized. A chain of SQEs will be broken, if any request in that
        /// chain ends in error. io_uring considers any unexpected result an error. This
        /// means that, eg, a short read will also terminate the remainder of the chain.
        /// If a chain of SQE links is broken, the remaining unstarted part of the chain
        /// will be terminated and completed with <code>-ECANCELED</code> as the error code.
        /// </summary>
        /// <remarks>Available since 5.3.</remarks>
        Link = 1 << 2, // IOSQE_IO_LINK

        /// <summary>
        /// Like IOSQE_IO_LINK, but it doesn't sever regardless of the completion result.
        /// Note that the link will still sever if we fail submitting the parent request,
        /// hard links are only resilient in the presence of completion results for
        /// requests that did submit correctly. <see cref="HardLink"/> implies <see cref="Link"/>.
        /// </summary>
        /// <remarks>Available since 5.5.</remarks>
        HardLink = 1 << 3, // IOSQE_IO_HARDLINK

        /// <summary>
        /// Normal operation for io_uring is to try and issue an sqe as non-blocking first,
        /// and if that fails, execute it in an async manner. To support more efficient
        /// overlapped operation of requests that the application knows/assumes will
        /// always (or most of the time) block, the application can ask for an sqe to be
        /// issued async from the start.
        /// </summary>
        /// <remarks>Available since 5.6</remarks>
        Async = 1 << 4, // IOSQE_ASYNC
    }
}