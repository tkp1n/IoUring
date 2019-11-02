using System.Threading;
using IoUring.Internal;

namespace IoUring
{
    public unsafe partial class Ring
    {
        private readonly uint _cqSize;

        private readonly CompletionQueue _cq;
        private readonly UnmapHandle _cqHandle;

        /// <summary>
        /// Checks whether a Completion Queue Event is available.
        /// </summary>
        /// <param name="result">The data from the observed Completion Queue Event if any</param>
        /// <returns>Whether a Completion Queue Event was observed</returns>
        public bool TryRead(ref Completion result)
        {
            // ensure we see everything the kernel manipulated prior to the head bump
            var head = Volatile.Read(ref *_cq.head); 

            _cq.AssertNoOverflows();
            
            if (head == *_cq.tail)
            {
                // No Completion Queue Event available
                return false;
            }

            var index = head & *_cq.ringMask;
            var cqe = &_cq.cqes[index];

            result = new Completion(cqe->res, cqe->user_data);

            // ensure the kernel can take notice of us reading the latest Event
            Volatile.Write(ref *_cq.head, head + 1); 
            return true;
        }
    }
}