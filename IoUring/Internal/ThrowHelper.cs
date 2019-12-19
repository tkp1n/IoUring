using System;
using System.Runtime.CompilerServices;
using static Tmds.Linux.LibC;

namespace IoUring.Internal
{
    internal static class ThrowHelper
    {
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

        public static void ThrowSubmissionEntryDroppedException(long count)
            => throw NewSubmissionEntryDroppedException(count);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception NewSubmissionEntryDroppedException(long count)
            => new SubmissionEntryDroppedException(count);

        public static void ThrowOverflowException(long count) 
            => throw NewCompletionQueueOverflowException(count);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception NewCompletionQueueOverflowException(long count)
            => new CompletionQueueOverflowException(count);

        public static void ThrowArgumentNullException(ExceptionArgument argument) 
            => throw NewArgumentNullException(argument);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Exception NewArgumentNullException(ExceptionArgument argument) 
            => new ArgumentNullException(argument.ToString());

        internal enum ExceptionArgument
        {
            continuation
        }
    }
}