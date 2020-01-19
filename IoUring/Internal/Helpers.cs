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
    }
}
