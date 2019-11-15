using System;
using System.Runtime.CompilerServices;

namespace IoUring.Internal
{
    internal static class Helpers
    {
        public static unsafe void* NULL => ((void*) 0);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe T* Add<T>(void* a, uint b) where T : unmanaged
        {
            return (T*) ((IntPtr) a + (int) b);
        }

        public static void VerifyPowerOfTwo(int a, string paramName, string message)
        {
            uint ua = (uint) a;
            if ((ua & (ua - 1)) != 0)
            {
                throw new ArgumentOutOfRangeException(paramName, message);
            }
        }
    }
}
