using System;

namespace IoUring.Concurrent
{
    [Flags]
    public enum ConcurrentSubmissionOption : byte
    {
        /// <summary>
        /// Marks this submission as independent of other submissions.
        /// </summary>
        None = 0x00,

        /// <summary>
        /// Wait for previously submitted items before the current one is issued.
        /// </summary>
        Drain = 1 << 1, // IOSQE_IO_DRAIN
    }
}