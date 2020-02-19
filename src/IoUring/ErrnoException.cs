using System;
using System.Runtime.InteropServices;
using static Tmds.Linux.LibC;

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
            const int bufferLength = 1024;
            byte* buffer = stackalloc byte[bufferLength];

            int rv = strerror_r(errno, buffer, bufferLength);

            return rv == 0 ? Marshal.PtrToStringAnsi((IntPtr)buffer)! : $"ERRNO: {errno}";
        }
    }
}
