using System.Runtime.CompilerServices;
using Tmds.Linux;
using IoUring.Internal;
using static Tmds.Linux.LibC;
using static IoUring.Internal.Helpers;
using static IoUring.Internal.ThrowHelper;

namespace IoUring
{
    public unsafe partial class Ring
    {
        private readonly SubmissionQueue _sq;
        private readonly UnmapHandle _sqHandle;
        private readonly UnmapHandle _sqeHandle;

        /// <summary>
        /// Adds a NOP to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareNop(ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            if (!TryPrepareNop(userData, options))
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
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareNop(ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            if (!NextSubmissionQueueEntry(out var sqe))
                return false;

            sqe->opcode = IORING_OP_NOP;
            sqe->flags = (byte) options;
            sqe->user_data = userData;

            return true;
        }

        /// <summary>
        /// Adds a readv, preadv or preadv2 to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to read from</param>
        /// <param name="iov">I/O vectors to read to</param>
        /// <param name="count">Number of I/O vectors</param>
        /// <param name="offset">Offset in bytes into the I/O vectors (as per preadv)</param>
        /// <param name="flags">Flags for the I/O (as per preadv2)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareReadV(
            int fd, iovec* iov, int count, off_t offset = default, int flags = 0,
            ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            if (!TryPrepareReadV(fd, iov, count, offset, flags, userData, options))
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
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareReadV(
            int fd, iovec* iov, int count, off_t offset = default, int flags = 0,
            ulong userData = 0, SubmissionOption options = SubmissionOption.None)
            => TryPrepareReadWrite(IORING_OP_READV, fd, iov, count, offset, flags, userData, options);

        /// <summary>
        /// Adds a writev, pwritev or pwritev2 to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to write to</param>
        /// <param name="iov">I/O vectors to write</param>
        /// <param name="count">Number of I/O vectors</param>
        /// <param name="offset">Offset in bytes into the I/O vectors (as per pwritev)</param>
        /// <param name="flags">Flags for the I/O (as per pwritev2)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareWriteV(
            int fd, iovec* iov, int count, off_t offset = default, int flags = 0,
            ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            if (!TryPrepareWriteV(fd, iov, count, offset, flags, userData, options))
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
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareWriteV(
            int fd, iovec* iov, int count, off_t offset = default, int flags = 0,
            ulong userData = 0, SubmissionOption options = SubmissionOption.None)
            => TryPrepareReadWrite(IORING_OP_WRITEV, fd, iov, count, offset, flags, userData, options);

        /// <summary>
        /// Adds a fsync to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to synchronize</param>
        /// <param name="fsyncOptions">Integrity options</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareFsync(int fd, FsyncOption fsyncOptions = FsyncOption.FileIntegrity,
            ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            if (!TryPrepareFsync(fd, fsyncOptions, userData, options))
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
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public bool TryPrepareFsync(int fd, FsyncOption fsyncOptions = FsyncOption.FileIntegrity,
            ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            if (!NextSubmissionQueueEntry(out var sqe))
                return false;

            sqe->opcode = IORING_OP_FSYNC;
            sqe->flags = (byte) options;
            sqe->fd = fd;
            sqe->fsync_flags = (uint) fsyncOptions;
            sqe->user_data = userData;

            return true;
        }

        /// <summary>
        /// Adds a read using a registered buffer/file to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to read from</param>
        /// <param name="buf">Buffers to read from</param>
        /// <param name="count">Number of buffers</param>
        /// <param name="index"></param>
        /// <param name="offset">Offset in bytes into buffer (as per preadv)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareReadFixed(int fd, void* buf, size_t count, int index, off_t offset = default,
            ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            if (!TryPrepareReadFixed(fd, buf, count, index, offset, userData, options))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add a read using a registered buffer/file to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to read from</param>
        /// <param name="buf">Buffers to read from</param>
        /// <param name="count">Number of buffers</param>
        /// <param name="index"></param>
        /// <param name="offset">Offset in bytes into buffer (as per preadv)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareReadFixed(int fd, void* buf, size_t count, int index, off_t offset = default,
            ulong userData = 0, SubmissionOption options = SubmissionOption.None)
            => TryPrepareReadWriteFixed(IORING_OP_READ_FIXED, fd, buf, count, index, offset, userData, options);

        /// <summary>
        /// Adds a write using a registered buffer/file to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to write to</param>
        /// <param name="buf">Buffers to write</param>
        /// <param name="count">Number of buffers</param>
        /// <param name="index"></param>
        /// <param name="offset">Offset in bytes into buffer (as per pwritev)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareWriteFixed(int fd, void* buf, size_t count, int index, off_t offset = default,
            ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            if (!TryPrepareWriteFixed(fd, buf, count, index, offset, userData, options))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add a write using a registered buffer/file to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to write to</param>
        /// <param name="buf">Buffers to write</param>
        /// <param name="count">Number of buffers</param>
        /// <param name="index"></param>
        /// <param name="offset">Offset in bytes into buffer (as per pwritev)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareWriteFixed(int fd, void* buf, size_t count, int index, off_t offset = default,
            ulong userData = 0, SubmissionOption options = SubmissionOption.None)
            => TryPrepareReadWriteFixed(IORING_OP_WRITE_FIXED, fd, buf, count, index, offset, userData, options);

        /// <summary>
        /// Adds a one-shot poll of the file descriptor to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to poll</param>
        /// <param name="pollEvents">Events to poll for</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PreparePollAdd(int fd, ushort pollEvents, ulong userData = 0,
            SubmissionOption options = SubmissionOption.None)
        {
            if (!TryPreparePollAdd(fd, pollEvents, userData, options))
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
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPreparePollAdd(int fd, ushort pollEvents, ulong userData = 0,
            SubmissionOption options = SubmissionOption.None)
        {
            if (!NextSubmissionQueueEntry(out var sqe))
                return false;

            sqe->opcode = IORING_OP_POLL_ADD;
            sqe->flags = (byte) options;
            sqe->fd = fd;
            sqe->poll_events = pollEvents;
            sqe->user_data = userData;

            return true;
        }

        /// <summary>
        /// Adds a request for removal of a previously added poll request to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PreparePollRemove(ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            if (!TryPreparePollRemove(userData, options))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add a request for removal of a previously added poll request to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPreparePollRemove(ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            if (!NextSubmissionQueueEntry(out var sqe))
                return false;

            sqe->opcode = IORING_OP_POLL_REMOVE;
            sqe->flags = (byte) options;
            sqe->user_data = userData;

            return true;
        }

        /// <summary>
        /// Adds a sync_file_range to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to sync</param>
        /// <param name="offset">Offset in bytes into the file</param>
        /// <param name="count">Number of bytes to sync</param>
        /// <param name="flags">Flags for the operation (as per sync_file_range)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareSyncFileRange(int fd, off_t offset, off_t count, uint flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            if (!TryPrepareSyncFileRange(fd, offset, count, flags, userData, options))
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
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareSyncFileRange(int fd, off_t offset, off_t count, uint flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            if (!NextSubmissionQueueEntry(out var sqe))
                return false;

            unchecked
            {
                sqe->opcode = IORING_OP_SYNC_FILE_RANGE;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->off = (ulong) (long) offset;
                sqe->len = (uint) count;
                sqe->sync_range_flags = flags;
                sqe->user_data = userData;
            }

            return true;
        }

        /// <summary>
        /// Adds a sendmsg to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to send to</param>
        /// <param name="msg">Message to send</param>
        /// <param name="flags">Flags for the operator (as per sendmsg)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareSendMsg(int fd, msghdr* msg, int flags, ulong userData = 0,
            SubmissionOption options = SubmissionOption.None)
        {
            if (!TryPrepareSendMsg(fd, msg, flags, userData, options))
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
        /// <param name="flags">Flags for the operator (as per sendmsg)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareSendMsg(int fd, msghdr* msg, int flags, ulong userData = 0,
            SubmissionOption options = SubmissionOption.None)
            => TryPrepareSendRecvMsg(IORING_OP_SENDMSG, fd, msg, flags, userData, options);

        /// <summary>
        /// Adds a recvmsg to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to receive from</param>
        /// <param name="msg">Message to read to</param>
        /// <param name="flags">Flags for the operator (as per recvmsg)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareRecvMsg(int fd, msghdr* msg, int flags, ulong userData = 0,
            SubmissionOption options = SubmissionOption.None)
        {
            if (!TryPrepareRecvMsg(fd, msg, flags, userData, options))
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
        /// <param name="flags">Flags for the operator (as per recvmsg)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareRecvMsg(int fd, msghdr* msg, int flags, ulong userData = 0,
            SubmissionOption options = SubmissionOption.None)
            => TryPrepareSendRecvMsg(IORING_OP_RECVMSG, fd, msg, flags, userData, options);

        /// <summary>
        /// Adds a timeout to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="ts">The amount of time after which the timeout should trigger if less than <paramref name="count"/> submissions completed.</param>
        /// <param name="count">The amount of completed submissions after which the timeout should trigger</param>
        /// <param name="timeoutOptions">Options on how <paramref name="ts"/> is interpreted</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareTimeout(timespec *ts, uint count = 1, TimeoutOptions timeoutOptions = TimeoutOptions.Relative, ulong userData = 0,
            SubmissionOption options = SubmissionOption.None)
        {
            if (!TryPrepareTimeout(ts, count, timeoutOptions, userData, options))
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
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareTimeout(timespec *ts, uint count = 1, TimeoutOptions timeoutOptions = TimeoutOptions.Relative, ulong userData = 0,
            SubmissionOption options = SubmissionOption.None) 
            => TryPrepareReadWrite(IORING_OP_TIMEOUT, -1, ts, 1, count, (int) timeoutOptions, userData, options);

        /// <summary>
        /// Adds the removal of a timeout to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="timeoutUserData">userData of the timeout submission that should be removed</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareTimeoutRemove(ulong timeoutUserData, ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            if (!TryPrepareTimeoutRemove(timeoutUserData, userData, options))
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
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareTimeoutRemove(ulong timeoutUserData, ulong userData = 0, SubmissionOption options = SubmissionOption.None)
            => TryPrepareReadWrite(IORING_OP_TIMEOUT_REMOVE, -1, (void*) timeoutUserData, 0, 0, 0, userData, options);

        /// <summary>
        /// Adds an accept to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to accept on</param>
        /// <param name="addr">(out) the address of the connected client.</param>
        /// <param name="addrLen">(out) the length of the address</param>
        /// <param name="flags">Flags as per accept4</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareAccept(int fd, sockaddr *addr, socklen_t *addrLen, int flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            if (!TryPrepareAccept(fd, addr, addrLen, flags, userData, options))
            {
                ThrowSubmissionQueueFullException();
            }
        }

        /// <summary>
        /// Attempts to add an accept to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">File descriptor to accept on</param>
        /// <param name="addr">(out) the address of the connected client.</param>
        /// <param name="addrLen">(out) the length of the address</param>
        /// <param name="flags">Flags as per accept4</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareAccept(int fd, sockaddr *addr, socklen_t *addrLen, int flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None) 
            => TryPrepareReadWrite(IORING_OP_ACCEPT, fd, addr, 0, (long) addrLen, flags, userData, options);

        /// <summary>
        /// Adds the cancellation of a previously submitted item to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="opUserData">userData of the operation to cancel</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareCancel(ulong opUserData, ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            if (!TryPrepareCancel(opUserData, userData, options))
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
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareCancel(ulong opUserData, ulong userData = 0, SubmissionOption options = SubmissionOption.None)
            => TryPrepareReadWrite(IORING_OP_ASYNC_CANCEL, -1, (void*) opUserData, 0, 0, 0, userData, options);

        /// <summary>
        /// Adds a connect to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="fd">The socket to connect on</param>
        /// <param name="addr">The address to connect to</param>
        /// <param name="addrLen">The length of the address</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareConnect(int fd, sockaddr* addr, socklen_t addrLen, ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            if (!TryPrepareConnect(fd, addr, addrLen, userData, options))
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
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareConnect(int fd, sockaddr* addr, socklen_t addrLen, ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            return TryPrepareReadWrite(IORING_OP_CONNECT, fd, addr, 0, (uint) addrLen, 0, userData, options);
        }

        /// <summary>
        /// Adds a timeout to a previously prepared linked item to the Submission Queue without it being submitted.
        /// The actual submission can be deferred to avoid unnecessary memory barriers.
        /// </summary>
        /// <param name="ts">The amount of time after which the timeout should trigger</param>
        /// <param name="timeoutOptions">Options on how <paramref name="ts"/> is interpreted</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareLinkTimeout(timespec* ts, TimeoutOptions timeoutOptions, ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            if (!TryPrepareLinkTimeout(ts, timeoutOptions, userData, options))
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
        /// <returns><code>false</code> if the submission queue is full. <code>true</code> otherwise.</returns>
        public bool TryPrepareLinkTimeout(timespec* ts, TimeoutOptions timeoutOptions = TimeoutOptions.Relative, ulong userData = 0, SubmissionOption options = SubmissionOption.None) 
            => TryPrepareReadWrite(IORING_OP_LINK_TIMEOUT, -1, ts, 1, 0, (int) timeoutOptions, userData, options);

        // internal for testing
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryPrepareReadWrite(byte op, int fd, void* iov, int count, off_t offset, int flags, ulong userData, SubmissionOption options)
        {
            if (!NextSubmissionQueueEntry(out var sqe))
                return false;

            unchecked
            {
                sqe->opcode = op;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->off = (ulong) (long) offset;
                sqe->addr = (ulong) iov;
                sqe->len = (uint) count;
                sqe->rw_flags = flags;
                sqe->user_data = userData;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryPrepareReadWriteFixed(byte op, int fd, void* buf, size_t count, int index, off_t offset, ulong userData, SubmissionOption options)
        {
            if (!NextSubmissionQueueEntry(out var sqe))
                return false;

            unchecked
            {
                sqe->opcode = op;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->off = (ulong) (long) offset;
                sqe->addr = (ulong) buf;
                sqe->len = (uint) count;
                sqe->buf_index = (ushort) index;
                sqe->user_data = userData;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool TryPrepareSendRecvMsg(byte op, int fd, msghdr* msg, int flags, ulong userData,
            SubmissionOption options)
        {
            if (!NextSubmissionQueueEntry(out var sqe))
                return false;

            unchecked
            {
                sqe->opcode = op;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->addr = (ulong) msg;
                sqe->len = 1;
                sqe->msg_flags = (uint) flags;
                sqe->user_data = userData;
            }

            return true;
        }

        /// <summary>
        /// Notifies the kernel of the availability of new Submission Queue Entries and waits for a given number of completions to occur.
        /// This typically requires a syscall and should be deferred as long as possible.
        /// </summary>
        /// <param name="minComplete">The number of completed Submission Queue Entries required before returning</param>
        /// <param name="operationsSubmitted">(out) The number of submitted Submission Queue Entries</param>
        /// <returns>The result of the operation</returns>
        /// <exception cref="ErrnoException">On negative result from syscall with errno other than EAGAIN, EBUSY and EINTR</exception>
        public SubmitResult SubmitAndWait(uint minComplete, out uint operationsSubmitted)
            => _sq.SubmitAndWait(_ringFd.DangerousGetHandle().ToInt32(), minComplete, out operationsSubmitted);

        /// <summary>
        /// Notifies the kernel of the availability of new Submission Queue Entries.
        /// This typically requires a syscall and should be deferred as long as possible.
        /// </summary>
        /// <param name="operationsSubmitted">(out) The number of submitted Submission Queue Entries</param>
        /// <returns>The result of the operation</returns>
        /// <exception cref="ErrnoException">On negative result from syscall with errno other than EAGAIN, EBUSY and EINTR</exception>
        public SubmitResult Submit(out uint operationsSubmitted)
            => SubmitAndWait(0, out operationsSubmitted);

        private bool NextSubmissionQueueEntry(out io_uring_sqe* sqe)
            => (sqe = _sq.NextSubmissionQueueEntry()) != NULL;
    }
}
