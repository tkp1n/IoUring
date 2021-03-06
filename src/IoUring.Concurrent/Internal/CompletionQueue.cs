using System.Threading;
using static IoUring.Internal.ThrowHelper;
using static IoUring.Internal.Helpers;
using Tmds.Linux;

namespace IoUring.Internal
{
    internal sealed unsafe partial class CompletionQueue
    {
        public bool TryRead(out Completion result)
        {
            uint head;
            uint next;
            uint tail;
            do
            {
                head = Volatile.Read(ref *_head);
                tail = *_tail;

                if (head == tail)
                {
                    result = default;
                    return false;
                }

                next = unchecked(head + 1);

                var index = head & _ringMask;
                var cqe = &_cqes[index];

                result = Completion.FromCqe(cqe);

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