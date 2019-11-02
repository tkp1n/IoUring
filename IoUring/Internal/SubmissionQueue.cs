using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Tmds.Linux;
using static Tmds.Linux.LibC;
using static IoUring.Internal.Helpers;

namespace IoUring.Internal
{
    internal unsafe class SubmissionQueue
    {
        /// <summary>
        /// Incremented by the kernel to let the application know, another element was consumed.
        /// </summary>
        public uint* head;

        /// <summary>
        /// Incremented by the application to let the kernel know, another element was submitted.
        /// </summary>
        public uint* tail;

        /// <summary>
        /// Mask to apply to potentially overflowing tail counter to get a valid index within the ring
        /// </summary>
        public uint* ringMask;

        /// <summary>
        /// Number of entries in the ring
        /// </summary>
        public uint* ringEntries;

        /// <summary>
        /// Set to IORING_SQ_NEED_WAKEUP by the kernel, if the Submission Queue polling thread is idle and needs
        /// a call to io_uring_enter with the IORING_ENTER_SQ_WAKEUP flag set.
        /// </summary>
        public uint* flags;

        /// <summary>
        /// Incremented by the kernel on each invalid submission.
        /// </summary>
        public uint* dropped;

        /// <summary>
        /// Array of indices within the <see cref="sqes"/>
        /// </summary>
        public uint* array;

        /// <summary>
        /// Submission Queue Entries to be filled by the application
        /// </summary>
        public io_uring_sqe* sqes;

        /// <summary>
        /// Index of the last Submission Queue Entry handed out to the application (to be filled).
        /// This is typically behind <see cref="tail"/> as the kernel must not yet know about bumps of the internal index, before the Entry is fully prepped
        /// </summary>
        public uint tailInternal;

        /// <summary>
        /// Index of the last Submission Queue Entry handed over to the kernel.
        /// This is typically ahead of <see cref="head"/> as the kernel might not have had the chance to consume the item at the given index.
        /// </summary>
        public uint headInternal;

        private SubmissionQueue(uint* head, uint* tail, uint* ringMask, uint* ringEntries, uint* flags, uint* dropped, uint* array, io_uring_sqe* sqes)
        {
            this.head = head;
            this.tail = tail;
            this.ringMask = ringMask;
            this.ringEntries = ringEntries;
            this.flags = flags;
            this.dropped = dropped;
            this.array = array;
            this.sqes = sqes;
            this.tailInternal = 0;
            this.headInternal = 0;
        }

        public static SubmissionQueue CreateSubmissionQueue(void* ringBase, io_sqring_offsets* offsets, io_uring_sqe* elements)
            => new SubmissionQueue(
                head: Add<uint>(ringBase, offsets->head),
                tail: Add<uint>(ringBase, offsets->tail),
                ringMask: Add<uint>(ringBase, offsets->ring_mask),
                ringEntries: Add<uint>(ringBase, offsets->ring_entries),
                flags: Add<uint>(ringBase, offsets->flags),
                dropped: Add<uint>(ringBase, offsets->dropped),
                array: Add<uint>(ringBase, offsets->array),
                sqes: elements
            );

        public bool IsFull => tailInternal - headInternal > *ringEntries;

        public uint EntriesToSubmit => tailInternal - headInternal;

        [Conditional("DEBUG")]
        public void AssertNoDroppedSubmissions() => Debug.Assert(Volatile.Read(ref *dropped) == 0);

        /// <summary>
        /// Finds the next Submission Queue Entry to be written to. The entry will be initialized with zeroes.
        /// If the Submission Queue is full, a null-pointer is returned.
        /// </summary>
        /// <returns>The next Submission Queue Entry to be written to or null if the Queue is full</returns>
        public io_uring_sqe* NextSubmissionQueueEntry()
        {
            if (IsFull)
            {
                return (io_uring_sqe*) NULL;
            }

            io_uring_sqe* sqe = &sqes[tailInternal & *ringMask];
            tailInternal = unchecked(tailInternal + 1); // natural overflow of uint is desired

            // Handout cleaned sqe
            Unsafe.InitBlockUnaligned(sqe, 0x00, (uint) sizeof(io_uring_sqe));
            return sqe;
        }

        public uint Submit()
        {
            uint gap = EntriesToSubmit;
            if (gap == 0) return 0;

            uint tailLocal = *tail;
            uint maskLocal = *ringMask;
            for (uint i = 0; i < gap; i++)
            {
                array[tailLocal & maskLocal] = headInternal & maskLocal;
                tailLocal++;
                head++;
            }

            // write barrier to ensure all manipulations above are visible to the kernel once the tail-bump is observed
            Volatile.Write(ref *tail, tailLocal);

            return gap;
        }

        private bool ShouldFlush(bool kernelSqPolling, out uint enterFlags)
        {
            enterFlags = 0;
            if (kernelSqPolling)
            {
                if ((*flags & IORING_SQ_NEED_WAKEUP) != 0)
                {
                    // Kernel is polling but transitioned to idle (IORING_SQ_NEED_WAKEUP)
                    enterFlags |= IORING_ENTER_SQ_WAKEUP;
                    return true;
                }

                // Kernel is still actively polling
                return false;
            }

            // If the kernel is not polling, we have to notify    
            return true;
        }

        public uint Flush(int ringFd, bool kernelIoPolling, uint toFlush, uint minComplete)
        {
            if (minComplete > toFlush) throw new ArgumentOutOfRangeException(nameof(minComplete), "must not be greater than toFlush");

            if (!ShouldFlush(kernelIoPolling, out uint enterFlags))
            {
                // Assume all Entries are known to the kernel (flushed)
                return toFlush;
            }

            if (minComplete > 0) enterFlags |= IORING_ENTER_GETEVENTS; // required for minComplete to take effect

            int res = io_uring_enter(ringFd, toFlush, minComplete, enterFlags, (sigset_t*) NULL);
            if (res < 0)
            {
                throw new ErrnoException(errno);
            }

            AssertNoDroppedSubmissions();
            
            return (uint)res;
        }
    }
}
