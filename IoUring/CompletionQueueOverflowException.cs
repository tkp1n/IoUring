using System;
using System.Runtime.Serialization;

namespace IoUring
{
    /// <summary>
    /// Exception thrown if the kernel produced more Completion Queue Events than the application was able to consume.
    /// This is typically the case, if Submission Queue Entries are produced faster than the consumption of their completions.
    /// By default the Completion Queue is sized twice the size of the Submission Queue to allow for some slack.
    /// It is however up to the application to ensure Completion Queue Events are consumed before the Queue overflows.
    /// </summary>
    public class CompletionQueueOverflowException : Exception
    {
        /// <summary>
        /// The number of overflowed Events 
        /// </summary>
        public long Overflow { get; }

        public CompletionQueueOverflowException(long overflow) : base($"Too many unconsumed Completion Queue Events: {overflow}")
        {
            Overflow = overflow;
        }

        protected CompletionQueueOverflowException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}