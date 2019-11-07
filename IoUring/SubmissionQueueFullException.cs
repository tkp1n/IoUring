using System;
using System.Runtime.Serialization;

namespace IoUring
{
    /// <summary>
    /// Exception thrown if an attempt was made to prepare a Submission Queue Entry whilst the Queue is full.
    /// </summary>
    public class SubmissionQueueFullException : Exception
    {
        public SubmissionQueueFullException() : base("Not enough free space to prepare current Submission Queue Entry")
        {
        }

        protected SubmissionQueueFullException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}