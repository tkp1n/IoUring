using System;

namespace IoUring
{
    public class ErrnoException : Exception
    {
        public ErrnoException(int errno) : base($"ERRNO: {errno}")
        {
            Errno = errno;
        }

        public int Errno { get; }
    }
}