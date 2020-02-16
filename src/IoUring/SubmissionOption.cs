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
        Drain = 1 << 1, // IOSQE_IO_DRAIN

        /// <summary>
        /// Marks items of a chain that must be executed sequentially.
        /// </summary>
        Link = 1 << 2, // IOSQE_IO_LINK

        /// <summary>
        /// Like <see cref="Link"/>, but it doesn't sever regardless of the completion result.
        /// </summary>
        HardLink = 1 << 3 // IOSQE_IO_HARDLINK
    }
}