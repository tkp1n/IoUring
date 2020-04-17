using System;
using IoUring.Internal;
using Tmds.Linux;
using static IoUring.Internal.ThrowHelper;

namespace IoUring.Concurrent
{
    public sealed unsafe class ConcurrentRing : BaseRing
    {
        public ConcurrentRing(int entries, RingOptions? options = null) : base(entries, options)
        {
            if (options != null && (options.EnablePolledIo || options.EnableSubmissionPolling))
                throw new NotSupportedException($"Polling options are not available for {nameof(ConcurrentRing)}");
        }

        /// <summary>
        /// Adds a NOP to the Submission Queue.
        /// </summary>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareNop(ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPrepareNop(userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add a NOP to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareNop(ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PrepareNop(userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

        /// <summary>
        /// Adds a readv, preadv or preadv2 to the Submission Queue.
        /// </summary>
        /// <param name="fd">File descriptor to read from</param>
        /// <param name="iov">I/O vectors to read to</param>
        /// <param name="count">Number of I/O vectors</param>
        /// <param name="offset">Offset in bytes into the I/O vectors (as per preadv)</param>
        /// <param name="flags">Flags for the I/O (as per preadv2)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareReadV(int fd, iovec* iov, int count, off_t offset = default, int flags = 0, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPrepareReadV(fd, iov, count, offset, flags, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add a readv, preadv or preadv2 to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to read from</param>
        /// <param name="iov">I/O vectors to read to</param>
        /// <param name="count">Number of I/O vectors</param>
        /// <param name="offset">Offset in bytes into the I/O vectors (as per preadv)</param>
        /// <param name="flags">Flags for the I/O (as per preadv2)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareReadV(int fd, iovec* iov, int count, off_t offset = default, int flags = 0, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PrepareReadV(fd, iov, count, offset, flags, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

        /// <summary>
        /// Adds a writev, pwritev or pwritev2 to the Submission Queue.
        /// </summary>
        /// <param name="fd">File descriptor to write to</param>
        /// <param name="iov">I/O vectors to write</param>
        /// <param name="count">Number of I/O vectors</param>
        /// <param name="offset">Offset in bytes into the I/O vectors (as per pwritev)</param>
        /// <param name="flags">Flags for the I/O (as per pwritev2)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareWriteV(int fd, iovec* iov, int count, off_t offset = default, int flags = 0, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPrepareWriteV(fd, iov, count, offset, flags, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add a writev, pwritev or pwritev2 to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to write to</param>
        /// <param name="iov">I/O vectors to write</param>
        /// <param name="count">Number of I/O vectors</param>
        /// <param name="offset">Offset in bytes into the I/O vectors (as per pwritev)</param>
        /// <param name="flags">Flags for the I/O (as per pwritev2)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareWriteV(int fd, iovec* iov, int count, off_t offset = default, int flags = 0, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PrepareWriteV(fd, iov, count, offset, flags, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

        /// <summary>
        /// Adds a fsync to the Submission Queue.
        /// </summary>
        /// <param name="fd">File descriptor to synchronize</param>
        /// <param name="fsyncOptions">Integrity options</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareFsync(int fd, FsyncOption fsyncOptions = FsyncOption.FileIntegrity, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPrepareFsync(fd, fsyncOptions, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add a fsync to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to synchronize</param>
        /// <param name="fsyncOptions">Integrity options</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareFsync(int fd, FsyncOption fsyncOptions = FsyncOption.FileIntegrity, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PrepareFsync(fd, fsyncOptions, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

        /// <summary>
        /// Adds a read using a registered buffer/file to the Submission Queue.
        /// </summary>
        /// <param name="fd">File descriptor to read from</param>
        /// <param name="buf">Buffer/file to read to</param>
        /// <param name="count">Number of bytes to read</param>
        /// <param name="index">Index of buffer/file</param>
        /// <param name="offset">Offset in bytes into the file descriptor (as per preadv)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareReadFixed(int fd, void* buf, size_t count, int index, off_t offset = default, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPrepareReadFixed(fd, buf, count, index, offset, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add a read using a registered buffer/file to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to read from</param>
        /// <param name="buf">Buffer/file to read to</param>
        /// <param name="count">Number of bytes to read</param>
        /// <param name="index">Index of buffer/file</param>
        /// <param name="offset">Offset in bytes into the file descriptor (as per preadv)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareReadFixed(int fd, void* buf, size_t count, int index, off_t offset = default, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PrepareReadFixed(fd, buf, count, index, offset, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

        /// <summary>
        /// Adds a write using a registered buffer/file to the Submission Queue.
        /// </summary>
        /// <param name="fd">File descriptor to write to</param>
        /// <param name="buf">Buffer/file to write</param>
        /// <param name="count">Number of bytes to write</param>
        /// <param name="index">Index of buffer/file</param>
        /// <param name="offset">Offset in bytes into the file descriptor (as per pwritev)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareWriteFixed(int fd, void* buf, size_t count, int index, off_t offset = default, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPrepareWriteFixed(fd, buf, count, index, offset, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add a write using a registered buffer/file to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to write to</param>
        /// <param name="buf">Buffer/file to write</param>
        /// <param name="count">Number of bytes to write</param>
        /// <param name="index">Index of buffer/file</param>
        /// <param name="offset">Offset in bytes into the file descriptor (as per pwritev)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareWriteFixed(int fd, void* buf, size_t count, int index, off_t offset = default, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PrepareWriteFixed(fd, buf, count, index, offset, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

        /// <summary>
        /// Adds a one-shot poll of the file descriptor to the Submission Queue.
        /// </summary>
        /// <param name="fd">File descriptor to poll</param>
        /// <param name="pollEvents">Events to poll for</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PreparePollAdd(int fd, ushort pollEvents, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPreparePollAdd(fd, pollEvents, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add a one-shot poll of the file descriptor to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to poll</param>
        /// <param name="pollEvents">Events to poll for</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPreparePollAdd(int fd, ushort pollEvents, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PreparePollAdd(fd, pollEvents, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

        /// <summary>
        /// Adds a request for removal of a previously added poll request to the Submission Queue.
        /// </summary>
        /// <param name="pollUserData">userData of the poll submission that should be removed</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PreparePollRemove(ulong pollUserData, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPreparePollRemove(pollUserData, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add a request for removal of a previously added poll request to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="pollUserData">userData of the poll submission that should be removed</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPreparePollRemove(ulong pollUserData, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PreparePollRemove(pollUserData, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

        /// <summary>
        /// Adds a sync_file_range to the Submission Queue.
        /// </summary>
        /// <param name="fd">File descriptor to sync</param>
        /// <param name="offset">Offset in bytes into the file</param>
        /// <param name="count">Number of bytes to sync</param>
        /// <param name="flags">Flags for the operation (as per sync_file_range)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareSyncFileRange(int fd, off_t offset, off_t count, uint flags, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPrepareSyncFileRange(fd, offset, count, flags, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add a sync_file_range to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to sync</param>
        /// <param name="offset">Offset in bytes into the file</param>
        /// <param name="count">Number of bytes to sync</param>
        /// <param name="flags">Flags for the operation (as per sync_file_range)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareSyncFileRange(int fd, off_t offset, off_t count, uint flags, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PrepareSyncFileRange(fd, offset, count, flags, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

        /// <summary>
        /// Adds a sendmsg to the Submission Queue.
        /// </summary>
        /// <param name="fd">File descriptor to send to</param>
        /// <param name="msg">Message to send</param>
        /// <param name="flags">Flags for the operation (as per sendmsg)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareSendMsg(int fd, msghdr* msg, uint flags, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPrepareSendMsg(fd, msg, flags, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add a sendmsg to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to send to</param>
        /// <param name="msg">Message to send</param>
        /// <param name="flags">Flags for the operation (as per sendmsg)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareSendMsg(int fd, msghdr* msg, uint flags, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PrepareSendMsg(fd, msg, flags, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

        /// <summary>
        /// Adds a recvmsg to the Submission Queue.
        /// </summary>
        /// <param name="fd">File descriptor to receive from</param>
        /// <param name="msg">Message to read to</param>
        /// <param name="flags">Flags for the operation (as per recvmsg)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareRecvMsg(int fd, msghdr* msg, uint flags, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPrepareRecvMsg(fd, msg, flags, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add a recvmsg to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to receive from</param>
        /// <param name="msg">Message to read to</param>
        /// <param name="flags">Flags for the operation (as per recvmsg)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareRecvMsg(int fd, msghdr* msg, uint flags, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PrepareRecvMsg(fd, msg, flags, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

        /// <summary>
        /// Adds a timeout to the Submission Queue.
        /// </summary>
        /// <param name="ts">The amount of time after which the timeout should trigger if less than <paramref name="count"/> submissions completed.</param>
        /// <param name="count">The amount of completed submissions after which the timeout should trigger</param>
        /// <param name="timeoutOptions">Options on how <paramref name="ts"/> is interpreted</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareTimeout(timespec* ts, uint count = 1, TimeoutOptions timeoutOptions = TimeoutOptions.Relative, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPrepareTimeout(ts, count, timeoutOptions, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add a timeout to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="ts">The amount of time after which the timeout should trigger if less than <paramref name="count"/> submissions completed.</param>
        /// <param name="count">The amount of completed submissions after which the timeout should trigger</param>
        /// <param name="timeoutOptions">Options on how <paramref name="ts"/> is interpreted</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareTimeout(timespec* ts, uint count = 1, TimeoutOptions timeoutOptions = TimeoutOptions.Relative, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PrepareTimeout(ts, count, timeoutOptions, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

        /// <summary>
        /// Adds the removal of a timeout to the Submission Queue.
        /// </summary>
        /// <param name="timeoutUserData">userData of the timeout submission that should be removed</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareTimeoutRemove(ulong timeoutUserData, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPrepareTimeoutRemove(timeoutUserData, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add the removal of a timeout to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="timeoutUserData">userData of the timeout submission that should be removed</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareTimeoutRemove(ulong timeoutUserData, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PrepareTimeoutRemove(timeoutUserData, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

        /// <summary>
        /// Adds an accept to the Submission Queue.
        /// </summary>
        /// <param name="fd">File descriptor to accept from</param>
        /// <param name="addr">(out) the address of the connected client.</param>
        /// <param name="addrLen">(out) the length of the address</param>
        /// <param name="flags">Flags as per accept4</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareAccept(int fd, sockaddr* addr, socklen_t* addrLen, int flags, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPrepareAccept(fd, addr, addrLen, flags, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add an accept to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to accept from</param>
        /// <param name="addr">(out) the address of the connected client.</param>
        /// <param name="addrLen">(out) the length of the address</param>
        /// <param name="flags">Flags as per accept4</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareAccept(int fd, sockaddr* addr, socklen_t* addrLen, int flags, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PrepareAccept(fd, addr, addrLen, flags, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

        /// <summary>
        /// Adds the cancellation of a previously submitted item to the Submission Queue.
        /// </summary>
        /// <param name="opUserData">userData of the operation to cancel</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareCancel(ulong opUserData, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPrepareCancel(opUserData, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add the cancellation of a previously submitted item to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="opUserData">userData of the operation to cancel</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareCancel(ulong opUserData, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PrepareCancel(opUserData, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

        /// <summary>
        /// Adds a connect to the Submission Queue.
        /// </summary>
        /// <param name="fd">The socket to connect on</param>
        /// <param name="addr">The address to connect to</param>
        /// <param name="addrLen">The length of the address</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareConnect(int fd, sockaddr* addr, socklen_t addrLen, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPrepareConnect(fd, addr, addrLen, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add a connect to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">The socket to connect on</param>
        /// <param name="addr">The address to connect to</param>
        /// <param name="addrLen">The length of the address</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareConnect(int fd, sockaddr* addr, socklen_t addrLen, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PrepareConnect(fd, addr, addrLen, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

        /// <summary>
        /// Adds a timeout to a previously prepared linked item to the Submission Queue.
        /// </summary>
        /// <param name="ts">The amount of time after which the timeout should trigger</param>
        /// <param name="timeoutOptions">Options on how <paramref name="ts"/> is interpreted</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareLinkTimeout(timespec* ts, TimeoutOptions timeoutOptions = TimeoutOptions.Relative, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPrepareLinkTimeout(ts, timeoutOptions, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add a timeout to a previously prepared linked item to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="ts">The amount of time after which the timeout should trigger</param>
        /// <param name="timeoutOptions">Options on how <paramref name="ts"/> is interpreted</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareLinkTimeout(timespec* ts, TimeoutOptions timeoutOptions = TimeoutOptions.Relative, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PrepareLinkTimeout(ts, timeoutOptions, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
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