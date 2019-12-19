using System;

namespace IoUring
{
    [Flags]
    public enum SubmissionOption : byte
    {
        /// <summary>
        /// Marks this submission as independent of other submissions.
        /// </summary>
        None = 0x00,
        
        /// <summary>
        /// Wait for previously submitted items before the current one is issued.
        /// </summary>
        Drain = 0x01, // IOSQE_IO_DRAIN
        
        /// <summary>
        /// Marks items of a chain that must be executed sequentially.
        /// </summary>
        Link = 0x02 // IOSQE_IO_LINK
    }
}