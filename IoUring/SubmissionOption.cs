using System;

namespace IoUring
{
    [Flags]
    public enum SubmissionOption : byte
    {
        None = 0x00,
        Drain = 0x01, // IOSQE_IO_DRAIN
        Link = 0x02 // IOSQE_IO_LINK
    }
}