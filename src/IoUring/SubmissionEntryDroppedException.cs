using System;
using System.Runtime.Serialization;

namespace IoUring
{
    /// <summary>
    /// Exception thrown if the Submission Queue reported that an Entry was dropped. This is the case, if the Entry was invalid.
    /// </summary>
    public class SubmissionEntryDroppedException : Exception
    {
        /// <summary>
        /// The number of dropped Entries
        /// </summary>
        public long Count { get; }
        public SubmissionEntryDroppedException(long count) : base($"Submission Queue dropped invalid Entries: {count.ToString()}")
        {
            Count = count;
        }

        protected SubmissionEntryDroppedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}