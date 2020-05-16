namespace IoUring.Internal
{
    internal enum SubmissionAcquireResult
    {
        SubmissionAcquired,
        TooManyOperationsInFlight,
        SubmissionQueueFull
    }
}