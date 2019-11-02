using System;

namespace IoUring
{
    public class ErrnoException : Exception
    {
        private readonly int _errno;
        
        public ErrnoException(int errno) : base($"ERRNO: {errno}")
        {
            _errno = errno;
        }
    }
}