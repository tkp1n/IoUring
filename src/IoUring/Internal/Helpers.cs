using System;
using System.Runtime.CompilerServices;
using Tmds.Linux;
using static Tmds.Linux.LibC;

namespace IoUring.Internal
{
    internal static class Helpers
    {
        public static unsafe void* NULL => (void*) 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe T* Add<T>(void* a, uint b) where T : unmanaged
        {
            return (T*) ((IntPtr) a + (int) b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void SafeEnter(int fd, uint toSubmit, uint minComplete, uint flags)
        {
            int res;
            int err = 0;
            do
            {
                res = io_uring_enter(fd, toSubmit, minComplete, flags, (sigset_t*) NULL);
            } while (res == -1 && (err = errno) == EINTR);

            if (res == -1) ThrowHelper.ThrowErrnoException(err);
        }
    }
}
