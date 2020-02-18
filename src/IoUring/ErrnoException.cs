using System;
using System.Runtime.InteropServices;
using Tmds.Linux;

namespace IoUring
{
    /// <summary>
    /// Exception thrown on errors during a syscall
    /// </summary>
    public class ErrnoException : Exception
    {
        public int Errno { get; }

        public ErrnoException(int errno) : base(GetErrorMessage(errno))
        {
            Errno = errno;
        }

        private unsafe static string GetErrorMessage(int errno)
        {
            int bufferLength = 1024;
            byte* buffer = stackalloc byte[bufferLength];

            int rv = LibC.strerror_r(errno, buffer, bufferLength);

            return rv == 0 ? Marshal.PtrToStringAnsi((IntPtr)buffer)! : $"ERRNO: {errno}";
        }
    }
}
