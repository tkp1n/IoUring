using System.Threading;
using static IoUring.Internal.ThrowHelper;
using static IoUring.Internal.Helpers;

namespace IoUring.Internal
{
    internal sealed unsafe partial class CompletionQueue
    {
        public bool TryRead(int ringFd, out Completion result)
        {
            uint head;
            uint next;
            uint tail;
            do
            {
StartLoop:
                head = Volatile.Read(ref *_head);
                tail = *_tail;

                if (head == tail)
                {
                    if (_ioPolled)
                    {
                        // If the kernel is polling I/O, we must reap completions.
                        PollCompletion(ringFd);
                        goto StartLoop;
                    }

                    result = default;
                    return false;
                }

                next = unchecked(head + 1);

                var index = head & _ringMask;
                var cqe = &_cqes[index];

                result = new Completion(cqe->res, cqe->user_data);

            } while (CompareExchange(ref *_head, next, head) != head);

            // piggy-back on the read-barrier above to verify that we have no overflows
            uint overflow = *_overflow;
            if (overflow > 0)
            {
                ThrowOverflowException(overflow);
            }

            return true;
        }
    }
}