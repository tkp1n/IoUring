using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Tmds.Linux;
using static Tmds.Linux.LibC;

namespace IoUring.Internal
{
    internal static unsafe class Helpers
    {
        public static unsafe void* NULL => (void*) 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* Add<T>(void* a, uint b) where T : unmanaged
        {
            return (T*) ((IntPtr) a + (int) b);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint CompareExchange(ref uint location, uint value, uint comparand)
        {
            unchecked
            {
                return (uint) Interlocked.CompareExchange(ref Unsafe.As<uint, int>(ref location), (int)value, (int)comparand);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SafeEnter(int fd, uint toSubmit, uint minComplete, uint flags)
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
