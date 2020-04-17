using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Tmds.Linux;
using static Tmds.Linux.LibC;
using static IoUring.Internal.ThrowHelper;

namespace IoUring.Internal
{
    public abstract unsafe partial class BaseRing
    {
        private static readonly bool[] SupportedOperationsByKernelVersion = {
            KernelVersion.Supports.IORING_OP_NOP,
            KernelVersion.Supports.IORING_OP_READV,
            KernelVersion.Supports.IORING_OP_WRITEV,
            KernelVersion.Supports.IORING_OP_FSYNC,
            KernelVersion.Supports.IORING_OP_READ_FIXED,
            KernelVersion.Supports.IORING_OP_WRITE_FIXED,
            KernelVersion.Supports.IORING_OP_POLL_ADD,
            KernelVersion.Supports.IORING_OP_POLL_REMOVE,
            KernelVersion.Supports.IORING_OP_SYNC_FILE_RANGE,
            KernelVersion.Supports.IORING_OP_SENDMSG,
            KernelVersion.Supports.IORING_OP_RECVMSG,
            KernelVersion.Supports.IORING_OP_TIMEOUT,
            KernelVersion.Supports.IORING_OP_TIMEOUT_REMOVE,
            KernelVersion.Supports.IORING_OP_ACCEPT,
            KernelVersion.Supports.IORING_OP_ASYNC_CANCEL,
            KernelVersion.Supports.IORING_OP_LINK_TIMEOUT,
            KernelVersion.Supports.IORING_OP_CONNECT,
            KernelVersion.Supports.IORING_OP_FALLOCATE,
            KernelVersion.Supports.IORING_OP_OPENAT,
            KernelVersion.Supports.IORING_OP_CLOSE,
            KernelVersion.Supports.IORING_OP_FILES_UPDATE,
            KernelVersion.Supports.IORING_OP_STATX,
            KernelVersion.Supports.IORING_OP_READ,
            KernelVersion.Supports.IORING_OP_WRITE,
            KernelVersion.Supports.IORING_OP_FADVISE,
            KernelVersion.Supports.IORING_OP_MADVISE,
            KernelVersion.Supports.IORING_OP_SEND,
            KernelVersion.Supports.IORING_OP_RECV,
            KernelVersion.Supports.IORING_OP_OPENAT2,
            KernelVersion.Supports.IORING_OP_EPOLL_CTL,
        };

        private static unsafe bool[] FetchSupportedOperations(int ringFd)
        {
            if (!KernelVersion.Supports.IORING_REGISTER_PROBE) return SupportedOperationsByKernelVersion;

            const int maxOps = 256; // The op code is a byte therefore we cannot see more than 256 ops
            int len = SizeOf.io_uring_probe + (maxOps * SizeOf.io_uring_probe_op);

            var probe = (io_uring_probe*) Marshal.AllocHGlobal(len);
            try
            {
                Unsafe.InitBlockUnaligned((byte*) probe, 0, (uint) len);

                int ret = io_uring_register(ringFd, IORING_REGISTER_PROBE, probe, maxOps);
                if (ret < 0) ThrowErrnoException();

                var ops = io_uring_probe.ops(probe);
                bool[] result = new bool[Math.Max(probe->last_op + 1, SupportedOperationsByKernelVersion.Length)];

                for (int i = 0; i < probe->ops_len; i++)
                {
                    result[ops[i].op] = (ops[i].flags & IO_URING_OP_SUPPORTED) != 0;
                }

                return result;
            }
            finally
            {
                Marshal.FreeHGlobal((IntPtr) probe);
            }
        }

        /// <summary>
        /// Returns whether the specified <see cref="RingOperation"/> is supported.
        /// </summary>
        public bool Supports(RingOperation operation)
        {
            var index = (uint) operation;
            var ops = _supportedOperations;
            return index < ops.Length && ops[index];
        }

        /// <summary>
        /// Whether protection against Completion Queue overflow is supported by the kernel.
        /// </summary>
        public bool SupportsNoDrop => (_features & IORING_FEAT_NODROP) != 0;

        /// <summary>
        /// Whether the application can be certain, that any data needed for async offload has been consumed by the
        /// kernel, when the Submission Queue Entry is consumed.
        /// </summary>
        public bool SupportsStableSubmits => (_features & IORING_FEAT_SUBMIT_STABLE) != 0;

        /// <summary>
        /// If this returns true, the application can specify <code>offset = -1</code> with read and write operations
        /// to mean current file position, which behaves like <code>preadv2</code> and <code>pwritev2</code>
        /// with <code>offset == -1</code>. It'll use (and update) the current file position. This obviously comes
        /// with the caveat that if the application has multiple reads or writes in flight,
        /// then the end result will not be as expected. This is similar to threads sharing
        /// a file descriptor and doing IO using the current file position.
        /// </summary>
        public bool SupportsReadWriteCurrentPosition => (_features & IORING_FEAT_RW_CUR_POS) != 0;

        /// <summary>
        /// If this flag is set, then <see cref="BaseRing"/> guarantees that both sync and async
        /// execution of a request assumes the credentials of the task that called
        /// <see cref="BaseRing.Submit(out uint)"/>/<see cref="BaseRing.SubmitAndWait(uint, out uint)"/>
        /// to queue the requests. If this flag isn't set, then requests are issued with
        /// the credentials of the task that originally created the <see cref="BaseRing"/>.
        /// If only one task is using a ring, then this flag doesn't matter as the credentials
        /// will always be the same. Note that this is the default behavior, tasks can
        /// still register different personalities through <see cref="BaseRing.RegisterPersonality"/>
        /// and specify the personality to use in the Submission Queue Entry.
        /// </summary>
        public bool SupportsCurrentPersonality => (_features & IORING_FEAT_CUR_PERSONALITY) != 0;
    }
}