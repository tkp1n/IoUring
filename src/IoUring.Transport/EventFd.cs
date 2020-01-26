using System.Runtime.InteropServices;

namespace IoUring.Transport
{
    public class EventFd
    {
        public const int EFD_SEMAPHORE = 1;
        public const int EFD_CLOEXEC = 524288;
        public const int EFD_NONBLOCK = 2048;
        
        [DllImport("libc.so.6", SetLastError = true)]
        public static extern int eventfd(int initval, int flags);
    }
}