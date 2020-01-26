using System;

namespace IoUring
{
    /// <summary>
    /// Exception thrown on errors during a syscall
    /// </summary>
    public class ErrnoException : Exception
    {
        public int Errno { get; }

        public ErrnoException(int errno) : base($"ERRNO: {errno}")
        {
            Errno = errno;
        }
    }
}