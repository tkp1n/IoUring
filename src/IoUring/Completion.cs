using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Tmds.Linux;
using static Tmds.Linux.LibC;

namespace IoUring
{
    /// <summary>
    /// Data associated with a Completion Queue Event
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Completion
    {
        private readonly ulong _userData;
        private readonly int _res;
        private readonly uint _flags;

        /// <summary>
        /// Return value of the completed operation or -errno
        /// </summary>
        public int Result => _res;

        /// <summary>
        /// User data supplied, when submitting the operation
        /// </summary>
        public ulong UserData => _userData;

        internal static unsafe Completion FromCqe(io_uring_cqe* cqe)
        {
            return Unsafe.AsRef<Completion>(cqe);
        }

        public void Deconstruct(out int result, out ulong userData)
        {
            result = _res;
            userData = _userData;
        }

        /// <summary>
        /// Returns, if present, the buffer ID selected by the kernel for the completed operation.
        /// </summary>
        /// <param name="bufferId">The buffer ID selected by the kernel for the completed operation</param>
        /// <returns>Whether the kernel selected a provided buffer for the completion of this operation.</returns>
        public bool TryGetBufferId(out int bufferId)
        {
            if ((_flags & IORING_CQE_F_BUFFER) == 0)
            {
                bufferId = default;
                return false;
            }

            bufferId = (int) (_flags >> IORING_CQE_BUFFER_SHIFT);
            return true;
        }
    }
}