using System;
using System.Runtime.InteropServices;
using Tmds.Linux;
using static Tmds.Linux.LibC;

namespace IoUring.Internal
{
    /// <summary>
    /// Wrapper of a file descriptor that should be disposed of using the "munmap" syscall
    /// </summary>
    internal unsafe class UnmapHandle : SafeHandle
    {
        private readonly size_t _size;

        public UnmapHandle(void* ptr, size_t size) : base(new IntPtr(MAP_FAILED), true)
        {
            _size = size;
            SetHandle((IntPtr) ptr);
        }

        protected override bool ReleaseHandle() => munmap(handle.ToPointer(), _size) == 0;

        public override bool IsInvalid => handle.ToPointer() == MAP_FAILED;
    }
}
