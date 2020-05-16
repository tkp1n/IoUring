using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static Tmds.Linux.LibC;

namespace IoUring.Internal
{
    internal static class ThrowHelper
    {
        public static void ThrowPlatformNotSupportedException()
            => throw NewPlatformNotSupportedException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception NewPlatformNotSupportedException()
            => new PlatformNotSupportedException();

        public static void ThrowErrnoException()
            => throw NewErrnoException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception NewErrnoException()
            => new ErrnoException(errno);

        public static void ThrowErrnoException(int errno)
            => throw NewErrnoException(errno);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception NewErrnoException(int errno)
            => new ErrnoException(errno);

        public static void ThrowSubmissionQueueFullException()
            => throw NewSubmissionQueueFullException();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception NewSubmissionQueueFullException()
            => new SubmissionQueueFullException();

        public static void ThrowOverflowException(long count)
            => throw NewCompletionQueueOverflowException(count);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception NewCompletionQueueOverflowException(long count)
            => new CompletionQueueOverflowException(count);

        public static void ThrowArgumentException(ExceptionArgument argument)
            => throw NewArgumentException(argument);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception NewArgumentException(ExceptionArgument argument)
            => new ArgumentException(argument.ToString());

        public static void ThrowArgumentNullException(ExceptionArgument argument)
            => throw NewArgumentNullException(argument);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception NewArgumentNullException(ExceptionArgument argument)
            => new ArgumentNullException(argument.ToString());

        public static void ThrowArgumentOutOfRangeException(ExceptionArgument argument)
            => throw NewArgumentOutOfRangeException(argument);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception NewArgumentOutOfRangeException(ExceptionArgument argument)
            => new ArgumentOutOfRangeException(argument.ToString());

        public static void ThrowSubmissionAcquisitionException(SubmissionAcquireResult result)
            => throw NewSubmissionAcquisitionException(result);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception NewSubmissionAcquisitionException(SubmissionAcquireResult result)
        {
            if (result == SubmissionAcquireResult.SubmissionQueueFull)
            {
                return new SubmissionQueueFullException();
            }
            else
            {
                Debug.Assert(result == SubmissionAcquireResult.TooManyOperationsInFlight);
                return new TooManyOperationsInFlightException();
            }
        }

        internal enum ExceptionArgument
        {
            entries,
            id,
            iovcnt,
            major,
            minor,
            nrFiles,
            obj,
            options,
            value
        }
    }
}