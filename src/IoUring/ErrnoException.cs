using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using static Tmds.Linux.LibC;

namespace IoUring
{
    /// <summary>
    /// Exception thrown on errors during a syscall
    /// </summary>
    public class ErrnoException : IOException
    {
        private static readonly Dictionary<int, string> ErrnoMessages = new Dictionary<int, string>();

        public int Errno => HResult;

        public ErrnoException(int errno) : base(GetErrorMessage(errno), errno) { }

        public static string GetErrorMessage(int errno)
        {
            lock (ErrnoMessages)
            {
                if (ErrnoMessages.TryGetValue(errno, out var message)) return message;

                message = CreateErrorMessage(errno);
                ErrnoMessages[errno] = message;
                return message;
            }
        }

        private static unsafe string CreateErrorMessage(int errno)
        {
            const int bufferLength = 1024;
            byte* buffer = stackalloc byte[bufferLength];

            int rv = strerror_r(errno, buffer, bufferLength);

            return rv == 0 ? Marshal.PtrToStringAnsi((IntPtr)buffer)! : $"ERRNO: {errno}";
        }
    }
}
