namespace IoUring
{
    /// <summary>
    /// The result of an attempt to submit I/O operations to the kernel.
    /// </summary>
    public enum SubmitResult
    {
        /// <summary>
        /// All prepared I/O operations were successfully submitted to the kernel.
        /// </summary>
        SubmittedSuccessfully,

        /// <summary>
        /// At least one prepared I/O operation was not yet consumed by the kernel.
        /// </summary>
        SubmittedPartially,

        /// <summary>
        /// The application must (wait for and) consume the completion of I/O operations before submitting new ones.
        /// </summary>
        AwaitCompletions
    }
}