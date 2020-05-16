using System;

namespace IoUring
{
    public sealed partial class Ring
    {
        /// <summary>
        /// Checks whether a Completion Queue Event is available.
        /// </summary>
        /// <param name="result">The data from the observed Completion Queue Event if any</param>
        /// <returns>Whether a Completion Queue Event was observed</returns>
        /// <exception cref="ErrnoException">If a syscall failed</exception>
        /// <exception cref="CompletionQueueOverflowException">If an overflow in the Completion Queue occurred</exception>
        public bool TryRead(out Completion result)
        {
            if (_cq.TryRead(_ringFd.DangerousGetHandle().ToInt32(), out result))
            {
                DecrementOperationsInFlight();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Reads, blocking if required, for a Completion Queue Event.
        /// </summary>
        /// <returns>The read Completion Queue Event</returns>
        /// <exception cref="ErrnoException">If a syscall failed</exception>
        /// <exception cref="CompletionQueueOverflowException">If an overflow in the Completion Queue occurred</exception>
        public Completion Read()
        {
            var completion = _cq.Read(_ringFd.DangerousGetHandle().ToInt32());
            DecrementOperationsInFlight();
            return completion;
        }

        /// <summary>
        /// Reads, blocking if required, for as many Completion Queue Events as fit the provided span.
        /// </summary>
        /// <param name="results">Buffer for the Completion Queue Events</param>
        /// <exception cref="ErrnoException">If a syscall failed</exception>
        /// <exception cref="CompletionQueueOverflowException">If an overflow in the Completion Queue occurred</exception>
        public void Read(Span<Completion> results)
        {
            _cq.Read(_ringFd.DangerousGetHandle().ToInt32(), results);
            DecrementOperationsInFlight(results.Length);
        }
    }
}