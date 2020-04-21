using System;
using IoUring.Internal;
using Tmds.Linux;
using static IoUring.Internal.ThrowHelper;

namespace IoUring.Concurrent
{
    public sealed unsafe partial class ConcurrentRing : BaseRing
    {
        public ConcurrentRing(int entries, RingOptions? options = null) : base(entries, options)
        {
            if (options != null && (options.EnablePolledIo || options.EnableSubmissionPolling))
                throw new NotSupportedException($"Polling options are not available for {nameof(ConcurrentRing)}");
        }

        /// <summary>
        /// Attempts to acquire a Submission Queue Entry to be prepared.
        /// </summary>
        /// <remarks>
        /// On success, the <see cref="Submission"/> must immediately be prepared and <see cref="Release">d</see>.
        /// Failure to do so will block all <see cref="Submission"/>s after the one acquired here.
        /// </remarks>
        /// <param name="submission">Submission Queue Entry to prepare</param>
        /// <returns>Whether a Submission Queue Entry could be acquired</returns>
        public bool TryAcquireSubmission(out Submission submission)
            => _sq.NextSubmissionQueueEntry(out submission);

        /// <summary>
        /// Acquires a Submission Queue Entry to be prepared.
        /// </summary>
        /// <remarks>
        /// On success, the <see cref="Submission"/> must immediately be prepared and <see cref="Release">d</see>.
        /// Failure to do so will block all <see cref="Submission"/>s after the one acquired here.
        /// </remarks>
        /// <param name="submission">Submission Queue Entry to prepare</param>
        /// <exception cref="SubmissionQueueFullException">If the Submission Queue is full</exception>
        public void AcquireSubmission(out Submission submission)
        {
            if (!TryAcquireSubmission(out submission)) ThrowSubmissionQueueFullException();
        }

        /// <summary>
        /// Attempts to acquire multiple Submission Queue Entries to be prepared.
        /// </summary>
        /// <remarks>
        /// On success, the <see cref="Submission"/>s must immediately be prepared and <see cref="Release">d</see>.
        /// Failure to do so will block all <see cref="Submission"/>s after the one acquired here.
        /// </remarks>
        /// <param name="submissions">Submission Queue Entries to prepare</param>
        /// <returns>Whether the Submission Queue Entries could be acquired</returns>
        public bool TryAcquireSubmissions(Span<Submission> submissions)
            => _sq.NextSubmissionQueueEntries(submissions);

        /// <summary>
        /// Acquires multiple Submission Queue Entries to be prepared.
        /// </summary>
        /// <remarks>
        /// On success, the <see cref="Submission"/>s must immediately be prepared and <see cref="Release">d</see>.
        /// Failure to do so will block all <see cref="Submission"/>s after the one acquired here.
        /// </remarks>
        /// <param name="submissions">Submission Queue Entries to prepare</param>
        /// <exception cref="SubmissionQueueFullException">If the Submission Queue is full</exception>
        public void AcquireSubmissions(Span<Submission> submissions)
        {
            if (!TryAcquireSubmissions(submissions)) ThrowSubmissionQueueFullException();
        }

        /// <summary>
        /// Marks this <see cref="Submission"/> as fully prepared and ready to be submitted.
        /// </summary>released
        /// <param name="submission">Submission Queue Entry that is fully prepared</param>
        public void Release(Submission submission)
            => _sq.NotifyPrepared(submission.Index);

        /// <summary>
        /// Checks whether a Completion Queue Event is available.
        /// </summary>
        /// <param name="result">The data from the observed Completion Queue Event if any</param>
        /// <returns>Whether a Completion Queue Event was observed</returns>
        /// <exception cref="ErrnoException">If a syscall failed</exception>
        /// <exception cref="CompletionQueueOverflowException">If an overflow in the Completion Queue occurred</exception>
        public bool TryRead(out Completion result)
            => _cq.TryRead(_ringFd.DangerousGetHandle().ToInt32(), out result);
    }
}