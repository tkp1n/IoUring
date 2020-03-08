using System;

namespace IoUring
{
    [Flags]
    public enum FsyncOption : uint
    {
        /// <summary>
        /// Meet requirements for synchronized I/O file integrity completion.
        /// </summary>
        FileIntegrity = 0,

        /// <summary>
        /// Meet requirements for synchronized I/O data integrity completion.
        /// </summary>
        DataIntegrity = 1 // IORING_FSYNC_DATASYNC
    }
}