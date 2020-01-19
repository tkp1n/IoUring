using System;
using IoUring.Internal;
using Tmds.Linux;
using static Tmds.Linux.LibC;

namespace IoUring
{
    public class RingOptions
    {
        /// <summary>
        /// Instructs the kernel to create a thread for polling.
        /// This is a privileged operation that will fail the setup with EPERM if the user doesn't have sufficient privilege.
        /// </summary>
        public bool EnableSubmissionPolling { get; set; } = false;

        /// <summary>
        /// Fixes the kernel Submission Queue polling thread to the given CPU.
        /// Ignored if value is negative, or <see cref="EnableSubmissionPolling"/> is false.
        /// </summary>
        public int SubmissionQueuePollingCpuAffinity { get; set; } = -1;
        
        /// <summary>
        /// The amount of time without I/O before the Submission Queue polling thread goes idle.
        /// The kernel defaults to one second of idle time before putting the thread to sleep.
        /// Ignored if set to <see cref="TimeSpan.Zero"/>, or if <see cref="EnableSubmissionPolling"/> is false.
        /// </summary>
        public TimeSpan PollingThreadIdleAfter { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Instructs the kernel to poll of I/O completions (instead of interrupt driven I/O)
        /// This is a privileged operation that will fail the setup with EPERM if the user doesn't have sufficient privilege.
        /// </summary>
        public bool EnablePolledIo { get; set; } = false;

        /// <summary>
        /// Override the kernel decision on the completion queue size.
        /// This option may not be supported by the kernel, which fill fail the setup with EINVAL.
        /// </summary>
        public int CompletionQueueSize { get; set; } = -1;

        internal unsafe void WriteTo(io_uring_params* p)
        {
            if (EnableSubmissionPolling)
            {
                p->flags |= IORING_SETUP_SQPOLL;
                if (SubmissionQueuePollingCpuAffinity >= 0)
                {
                    p->flags |= IORING_SETUP_SQ_AFF;
                    p->sq_thread_cpu = (uint) SubmissionQueuePollingCpuAffinity;
                }

                if (PollingThreadIdleAfter != TimeSpan.Zero)
                {
                    p->sq_thread_idle = (uint) PollingThreadIdleAfter.Milliseconds;
                }
            }

            if (EnablePolledIo)
            {
                p->flags |= IORING_SETUP_IOPOLL;
            }

            if (CompletionQueueSize >= 0)
            {
                p->cq_entries = (uint) CompletionQueueSize;
            }
        }
    }
}
