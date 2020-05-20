using System.Threading;
using static Tmds.Linux.LibC;
using static IoUring.Internal.Helpers;
using static IoUring.Internal.ThrowHelper;
using System.Runtime.CompilerServices;

namespace IoUring.Internal
{
    internal sealed unsafe partial class CompletionQueue
    {
        public bool TryRead(out Completion result)
        {
            var ok = TryReadInternal(out result) || TryReadSlow(out result);
            UpdateHead();
            return ok;
        }

        public Completion Read(int ringFd)
        {
            while (true)
            {
                SafeEnter(ringFd, 0, 1, IORING_ENTER_GETEVENTS);
                if (TryRead(out var completion))
                {
                    return completion;
                }
            }
        }

        private bool TryReadInternal(out Completion result)
        {
            var head = *_head;

            // Try read from internal tail to avoid memory barrier
            if (head == *_tailInternal)
            {
                result = default;
                return false;
            }

            PrepareCompletion(out result, head);
            return true;
        }

        private bool TryReadSlow(out Completion result)
        {
            var head = *_head;

            if (_ioPolled)
            {
                // If the kernel is polling I/O, we must reap completions.
                PollCompletion();
            }

            // check with a memory barrier to ensure we see everything the kernel manipulated prior to the tail bump
            var eventsAvailable = head != Volatile.Read(ref *_tail);

            // piggy-back on the read-barrier above to verify that we have no overflows
            var overflow = *_overflow;
            if (overflow > 0)
            {
                ThrowOverflowException(overflow);
            }

            if (!eventsAvailable)
            {
                result = default;
                return false;
            }

            // Update internal tail now, earlier would be pointless if eventsAvailable == false
            _tailInternal = _tail;

            PrepareCompletion(out result, head);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PrepareCompletion(out Completion result, uint head)
        {
            var index = head & _ringMask;
            var cqe = &_cqes[index];

            result = new Completion(cqe->res, cqe->user_data);

            *_headInternal = unchecked(*_headInternal + 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UpdateHead() => Volatile.Write(ref *_head, *_headInternal);
    }
}
