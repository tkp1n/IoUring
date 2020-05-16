using System;
using System.Runtime.Serialization;

namespace IoUring
{
    /// <summary>
    /// Exception thrown if the kernel does not support IORING_FEAT_NODROP and too many operations are currently in flight.
    /// </summary>
    public class TooManyOperationsInFlightException : Exception
    {
        public TooManyOperationsInFlightException() : base("Too many operations are currently in flight")
        {
        }

        protected TooManyOperationsInFlightException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}