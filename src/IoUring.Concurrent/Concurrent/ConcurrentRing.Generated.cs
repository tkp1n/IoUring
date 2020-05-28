using IoUring.Internal;
using Tmds.Linux;
using static IoUring.Internal.ThrowHelper;

namespace IoUring.Concurrent
{
    public sealed unsafe partial class ConcurrentRing
    {
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
        /// <param name="offset">Offset at which the I/O is performed (as per preadv)</param>
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
        /// <param name="offset">Offset at which the I/O is performed (as per preadv)</param>
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
        /// <param name="offset">Offset at which the I/O is performed (as per pwritev)</param>
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
        /// <param name="offset">Offset at which the I/O is performed (as per pwritev)</param>
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
        /// <param name="offset">Offset at which the I/O is performed (as per preadv)</param>
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
        /// <param name="offset">Offset at which the I/O is performed (as per preadv)</param>
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
        /// <param name="offset">Offset at which the I/O is performed (as per pwritev)</param>
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
        /// <param name="offset">Offset at which the I/O is performed (as per pwritev)</param>
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
        /// Adds a fallocate to the Submission Queue.
        /// </summary>
        /// <param name="fd">The file to manipulate the allocated disk space for</param>
        /// <param name="mode">The operation to be performed</param>
        /// <param name="offset">Offset in bytes into the file</param>
        /// <param name="len">Number of bytes to operate on</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareFallocate(int fd, int mode, off_t offset, off_t len, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPrepareFallocate(fd, mode, offset, len, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add a fallocate to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">The file to manipulate the allocated disk space for</param>
        /// <param name="mode">The operation to be performed</param>
        /// <param name="offset">Offset in bytes into the file</param>
        /// <param name="len">Number of bytes to operate on</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareFallocate(int fd, int mode, off_t offset, off_t len, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PrepareFallocate(fd, mode, offset, len, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

        /// <summary>
        /// Adds a closeat to the Submission Queue.
        /// </summary>
        /// <param name="dfd">Directory file descriptor</param>
        /// <param name="path">Path to be opened</param>
        /// <param name="flags">Flags for the open operation (e.g. access mode)</param>
        /// <param name="mode">File mode bits</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareOpenAt(int dfd, byte* path, int flags, mode_t mode = default, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPrepareOpenAt(dfd, path, flags, mode, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add a closeat to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="dfd">Directory file descriptor</param>
        /// <param name="path">Path to be opened</param>
        /// <param name="flags">Flags for the open operation (e.g. access mode)</param>
        /// <param name="mode">File mode bits</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareOpenAt(int dfd, byte* path, int flags, mode_t mode = default, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PrepareOpenAt(dfd, path, flags, mode, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

        /// <summary>
        /// Adds a close to the Submission Queue.
        /// </summary>
        /// <param name="fd">File descriptor to close</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareClose(int fd, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPrepareClose(fd, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add a close to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to close</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareClose(int fd, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PrepareClose(fd, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

        /// <summary>
        /// Adds an update to the registered files to the Submission Queue.
        /// </summary>
        /// <param name="fds">File descriptors to add / -1 to remove</param>
        /// <param name="nrFds">Number of changing file descriptors</param>
        /// <param name="offset">Offset into the previously registered files</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareFilesUpdate(int* fds, int nrFds, int offset, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPrepareFilesUpdate(fds, nrFds, offset, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add an update to the registered files to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fds">File descriptors to add / -1 to remove</param>
        /// <param name="nrFds">Number of changing file descriptors</param>
        /// <param name="offset">Offset into the previously registered files</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareFilesUpdate(int* fds, int nrFds, int offset, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PrepareFilesUpdate(fds, nrFds, offset, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

        /// <summary>
        /// Adds a statx to the Submission Queue.
        /// </summary>
        /// <param name="dfd">Directory file descriptor for relative paths</param>
        /// <param name="path">Absolute or relative path</param>
        /// <param name="flags">Influence pathname-based lookup</param>
        /// <param name="mask">Identifies the required fields</param>
        /// <param name="statxbuf">Buffer for the required information</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareStatx(int dfd, byte* path, int flags, uint mask, statx* statxbuf, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPrepareStatx(dfd, path, flags, mask, statxbuf, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add a statx to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="dfd">Directory file descriptor for relative paths</param>
        /// <param name="path">Absolute or relative path</param>
        /// <param name="flags">Influence pathname-based lookup</param>
        /// <param name="mask">Identifies the required fields</param>
        /// <param name="statxbuf">Buffer for the required information</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareStatx(int dfd, byte* path, int flags, uint mask, statx* statxbuf, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PrepareStatx(dfd, path, flags, mask, statxbuf, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

        /// <summary>
        /// Adds a read to the Submission Queue.
        /// </summary>
        /// <param name="fd">File descriptor</param>
        /// <param name="buf">Buffer to read to</param>
        /// <param name="nbytes">Number of bytes to read</param>
        /// <param name="offset">Offset at which the I/O is performed</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareRead(int fd, void* buf, uint nbytes, off_t offset, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPrepareRead(fd, buf, nbytes, offset, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add a read to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor</param>
        /// <param name="buf">Buffer to read to</param>
        /// <param name="nbytes">Number of bytes to read</param>
        /// <param name="offset">Offset at which the I/O is performed</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareRead(int fd, void* buf, uint nbytes, off_t offset, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PrepareRead(fd, buf, nbytes, offset, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

        /// <summary>
        /// Adds a write to the Submission Queue.
        /// </summary>
        /// <param name="fd">File descriptor</param>
        /// <param name="buf">Buffer to write</param>
        /// <param name="nbytes">Number of bytes to write</param>
        /// <param name="offset">Offset at which the I/O is performed</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareWrite(int fd, void* buf, uint nbytes, off_t offset, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPrepareWrite(fd, buf, nbytes, offset, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add a write to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor</param>
        /// <param name="buf">Buffer to write</param>
        /// <param name="nbytes">Number of bytes to write</param>
        /// <param name="offset">Offset at which the I/O is performed</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareWrite(int fd, void* buf, uint nbytes, off_t offset, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PrepareWrite(fd, buf, nbytes, offset, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

        /// <summary>
        /// Adds a posix_fadvise to the Submission Queue.
        /// </summary>
        /// <param name="fd">File descriptor</param>
        /// <param name="offset">Offset into the file</param>
        /// <param name="len">Length of the file range</param>
        /// <param name="advice">Advice for the file range</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareFadvise(int fd, off_t offset, off_t len, int advice, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPrepareFadvise(fd, offset, len, advice, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add a posix_fadvise to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor</param>
        /// <param name="offset">Offset into the file</param>
        /// <param name="len">Length of the file range</param>
        /// <param name="advice">Advice for the file range</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareFadvise(int fd, off_t offset, off_t len, int advice, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PrepareFadvise(fd, offset, len, advice, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

        /// <summary>
        /// Adds an madvise to the Submission Queue.
        /// </summary>
        /// <param name="addr">Start of address range</param>
        /// <param name="len">Length of address range</param>
        /// <param name="advice">Advice for address range</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareMadvise(void* addr, off_t len, int advice, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPrepareMadvise(addr, len, advice, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add an madvise to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="addr">Start of address range</param>
        /// <param name="len">Length of address range</param>
        /// <param name="advice">Advice for address range</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareMadvise(void* addr, off_t len, int advice, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PrepareMadvise(addr, len, advice, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

        /// <summary>
        /// Adds a send to the Submission Queue.
        /// </summary>
        /// <param name="sockfd">Socket file descriptor</param>
        /// <param name="buf">Buffer to send</param>
        /// <param name="len">Length of buffer to send</param>
        /// <param name="flags">Flags for the send</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareSend(int sockfd, void* buf, size_t len, int flags, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPrepareSend(sockfd, buf, len, flags, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add a send to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="sockfd">Socket file descriptor</param>
        /// <param name="buf">Buffer to send</param>
        /// <param name="len">Length of buffer to send</param>
        /// <param name="flags">Flags for the send</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareSend(int sockfd, void* buf, size_t len, int flags, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PrepareSend(sockfd, buf, len, flags, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

        /// <summary>
        /// Adds a recv to the Submission Queue.
        /// </summary>
        /// <param name="sockfd">Socket file descriptor</param>
        /// <param name="buf">Buffer to read to</param>
        /// <param name="len">Length of buffer to read to</param>
        /// <param name="flags">Flags for the recv</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareRecv(int sockfd, void* buf, size_t len, int flags, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPrepareRecv(sockfd, buf, len, flags, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add a recv to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="sockfd">Socket file descriptor</param>
        /// <param name="buf">Buffer to read to</param>
        /// <param name="len">Length of buffer to read to</param>
        /// <param name="flags">Flags for the recv</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareRecv(int sockfd, void* buf, size_t len, int flags, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PrepareRecv(sockfd, buf, len, flags, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

        /// <summary>
        /// Adds an openat2 to the Submission Queue.
        /// </summary>
        /// <param name="dfd">Directory file descriptor</param>
        /// <param name="path">Path to be opened</param>
        /// <param name="how">How pat should be opened</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareOpenAt2(int dfd, byte* path, open_how* how, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPrepareOpenAt2(dfd, path, how, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add an openat2 to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="dfd">Directory file descriptor</param>
        /// <param name="path">Path to be opened</param>
        /// <param name="how">How pat should be opened</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareOpenAt2(int dfd, byte* path, open_how* how, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PrepareOpenAt2(dfd, path, how, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

        /// <summary>
        /// Adds an epoll_ctl to the Submission Queue.
        /// </summary>
        /// <param name="epfd">epoll instance file descriptor</param>
        /// <param name="fd">File descriptor</param>
        /// <param name="op">Operation to be performed for the file descriptor</param>
        /// <param name="ev">Settings</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareEpollCtl(int epfd, int fd, int op, epoll_event* ev, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryPrepareEpollCtl(epfd, fd, op, ev, userData, options, personality))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add an epoll_ctl to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="epfd">epoll instance file descriptor</param>
        /// <param name="fd">File descriptor</param>
        /// <param name="op">Operation to be performed for the file descriptor</param>
        /// <param name="ev">Settings</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareEpollCtl(int epfd, int fd, int op, epoll_event* ev, ulong userData = 0, ConcurrentSubmissionOption options = ConcurrentSubmissionOption.None, ushort personality = 0)
        {
            if (!TryAcquireSubmission(out var submission))
                return false;

            submission.PrepareEpollCtl(epfd, fd, op, ev, userData, (SubmissionOption) options, personality);

            Release(submission);
            return true;
        }

    }
}
