using System;
using System.Runtime.InteropServices;
using static Tmds.Linux.LibC;

namespace IoUring.Internal
{
    /// <summary>
    /// Wrapper of a file descriptor that should be disposed of using the "close" syscall
    /// </summary>
    internal class CloseHandle : SafeHandle
    {
        public CloseHandle() : base(new IntPtr(-1), true)
        { }

        public void SetHandle(int fd) => SetHandle((IntPtr)fd);

        protected override bool ReleaseHandle()
        {
            // close(2) on Linux should not be checked other than for diagnostic purposes
            close(handle.ToInt32());
            return true;
        }

        public override bool IsInvalid => handle.ToInt32() < 0;
    }
}
