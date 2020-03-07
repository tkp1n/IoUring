using System;
using System.Threading;
using static Tmds.Linux.LibC;
using static IoUring.Internal.Helpers;
using static IoUring.Internal.ThrowHelper;

namespace IoUring.Internal
{
    internal sealed unsafe partial class CompletionQueue
    {
        public bool TryRead(int ringFd, out Completion result)
            => TryRead(ringFd, out result, true);

        public Completion Read(int ringFd)
        {
            while (true)
            {
                SafeEnter(ringFd, 0, 1, IORING_ENTER_GETEVENTS);
                if (TryRead(ringFd, out var completion, true))
                {
                    return completion;
                }
            }
        }

        public void Read(int ringFd, Span<Completion> results)
        {
            int read = 0;
            while (read < results.Length)
            {
                // Head is moved below to avoid memory barrier in loop
                if (TryRead(read, out results[read], false))
                {
                    read++;
                    continue; // keep on reading without syscall-ing
                }

                SafeEnter(ringFd, 0, (uint) (results.Length - read), IORING_ENTER_GETEVENTS);
            }

            // Move head now, as we skipped the memory barrier in the TryRead above
            Volatile.Write(ref *_head, *_headInternal);
        }

        private bool TryRead(int ringFd, out Completion result, bool bumpHead)
        {
            uint head = *_head;

            // Try read from internal tail first to avoid memory barrier
            bool eventsAvailable = head != *_tailInternal;

            if (!eventsAvailable)
            {
                if (_ioPolled)
                {
                    // If the kernel is polling I/O, we must reap completions.
                    PollCompletion(ringFd);
                }

                // double-check with a memory barrier to ensure we see everything the kernel manipulated prior to the tail bump
                eventsAvailable = head != Volatile.Read(ref *_tail);
                _tailInternal = _tail;

                // piggy-back on the read-barrier above to verify that we have no overflows
                uint overflow = *_overflow;
                if (overflow > 0)
                {
                    ThrowOverflowException(overflow);
                }
            }

            if (!eventsAvailable)
            {
                result = default;
                return false;
            }

            var index = head & _ringMask;
            var cqe = &_cqes[index];

            result = new Completion(cqe->res, cqe->user_data);

            *_headInternal = unchecked(*_headInternal + 1);
            if (bumpHead)
            {
                // ensure the kernel can take notice of us consuming the Events
                Volatile.Write(ref *_head, *_headInternal);
            }

            return true;
        }
    }
}
