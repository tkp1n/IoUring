using System;
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
            var sqe = _sq.NextSubmissionQueueEntry();
            if (sqe == NULL) return false;

            sqe->opcode = IORING_OP_NOP;
            sqe->user_data = userData;
            sqe->flags = (byte) options;

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
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="count"/> or <paramref name="offset"/> is negative</exception>
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
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="count"/> or <paramref name="offset"/> is negative</exception>
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
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="count"/> or <paramref name="offset"/> is negative</exception>
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
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="count"/> or <paramref name="offset"/> is negative</exception>
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
            var sqe = _sq.NextSubmissionQueueEntry();
            if (sqe == NULL) return false;

            sqe->opcode = IORING_OP_FSYNC;
            sqe->user_data = userData;
            sqe->flags = (byte) options;
            sqe->fd = fd;
            sqe->fsync_flags = (uint) fsyncOptions;

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
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="count"/>, <paramref name="index"/> or <paramref name="offset"/> is negative</exception>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareRead(int fd, void* buf, size_t count, int index, off_t offset = default,
            ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            if (!TryPrepareRead(fd, buf, count, index, offset, userData, options))
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
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="count"/>, <paramref name="index"/> or <paramref name="offset"/> is negative</exception>
        public bool TryPrepareRead(int fd, void* buf, size_t count, int index, off_t offset = default,
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
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="count"/>, <paramref name="index"/> or <paramref name="offset"/> is negative</exception>
        /// <exception cref="SubmissionQueueFullException">If no more free space in the Submission Queue is available</exception>
        public void PrepareWrite(int fd, void* buf, size_t count, int index, off_t offset = default,
            ulong userData = 0, SubmissionOption options = SubmissionOption.None)
        {
            if (!TryPrepareWrite(fd, buf, count, index, offset, userData, options))
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
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="count"/>, <paramref name="index"/> or <paramref name="offset"/> is negative</exception>
        public bool TryPrepareWrite(int fd, void* buf, size_t count, int index, off_t offset = default,
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
            var sqe = _sq.NextSubmissionQueueEntry();
            if (sqe == NULL) return false;

            sqe->opcode = IORING_OP_POLL_ADD;
            sqe->user_data = userData;
            sqe->flags = (byte) options;
            sqe->fd = fd;
            sqe->poll_events = pollEvents;

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
            var sqe = _sq.NextSubmissionQueueEntry();
            if (sqe == NULL) return false;

            sqe->opcode = IORING_OP_POLL_REMOVE;
            sqe->user_data = userData;
            sqe->flags = (byte) options;

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
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> or <paramref name="count"/> is negative</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="count"/> is not less than 2^32</exception>
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
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="offset"/> or <paramref name="count"/> is negative</exception>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="count"/> is not less than 2^32</exception>
        public bool TryPrepareSyncFileRange(int fd, off_t offset, off_t count, uint flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None) 
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "must be non-negative");
            if (count > uint.MaxValue) throw new ArgumentOutOfRangeException(nameof(count), "must be less than 2^32");
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), "must be non-negative");

            var sqe = _sq.NextSubmissionQueueEntry();
            if (sqe == NULL) return false;

            sqe->opcode = IORING_OP_SYNC_FILE_RANGE;
            sqe->user_data = userData;
            sqe->flags = (byte) options;
            sqe->fd = fd;
            sqe->len = (uint) count;
            sqe->off = (ulong)(long) offset;
            sqe->sync_range_flags = flags;

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
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="flags"/> is negative</exception>
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
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="flags"/> is negative</exception>
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
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="flags"/> is negative</exception>
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
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="flags"/> is negative</exception>
        public bool TryPrepareRecvMsg(int fd, msghdr* msg, int flags, ulong userData = 0,
            SubmissionOption options = SubmissionOption.None)
            => TryPrepareSendRecvMsg(IORING_OP_RECVMSG, fd, msg, flags, userData, options);
   
        private bool TryPrepareReadWrite(byte op, int fd, void* iov, int count, off_t offset, int flags, ulong userData, SubmissionOption options)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "must be non-negative");
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), "must be non-negative");

            var sqe = _sq.NextSubmissionQueueEntry();
            if (sqe == NULL) return false;

            sqe->opcode = op;
            sqe->user_data = userData;
            sqe->flags = (byte) options;
            sqe->fd = fd;
            sqe->addr = (ulong) iov;
            sqe->len = (uint) count;
            sqe->off = (ulong)(long) offset;
            sqe->rw_flags = flags;

            return true;
        }

        private bool TryPrepareReadWriteFixed(byte op, int fd, void* buf, size_t count, int index, off_t offset, ulong userData, SubmissionOption options)
        {            
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "must be non-negative");
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), "must be non-negative");
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), "must be non-negative");

            var sqe = _sq.NextSubmissionQueueEntry();
            if (sqe == NULL) return false;

            sqe->opcode = op;
            sqe->user_data = userData;
            sqe->flags = (byte) options;
            sqe->fd = fd;
            sqe->addr = (ulong) buf;
            sqe->len = (uint) count;
            sqe->off = (ulong)(long) offset;
            sqe->buf_index = (ushort) index;

            return true;
        }

        private bool TryPrepareSendRecvMsg(byte op, int fd, msghdr* msg, int flags, ulong userData,
            SubmissionOption options)
        {
            if (flags < 0) throw new ArgumentOutOfRangeException(nameof(flags), "must be non-negative");

            var sqe = _sq.NextSubmissionQueueEntry();
            if (sqe == NULL) return false;

            sqe->opcode = op;
            sqe->user_data = userData;
            sqe->flags = (byte) options;
            sqe->fd = fd;
            sqe->addr = (ulong) msg;
            sqe->len = 1;
            sqe->msg_flags = (uint) flags;

            return true;
        }

        /// <summary>
        /// Submits all pending (not previously submitted) Submission Queue Entries to the kernel.
        /// Submitting an Entry makes it available to the kernel if polling is enabled. Otherwise, the submission
        /// will be ignored until flushed.
        /// It is profitable to submit as many Entries as possible before performing the Flush, as the Flush operation includes a syscall.
        /// </summary>
        /// <returns>The number of submitted Entries</returns>
        public uint Submit() => _sq.Submit();

        /// <summary>
        /// Notifies the kernel of the availability of new Submission Queue Entries.
        /// This typically requires a syscall and should be deferred as long as possible. 
        /// </summary>
        /// <param name="toFlush">Number of un-flushed Submission Queue Entries</param>
        /// <param name="minComplete">The number of completed Submission Queue Entries required before returning (default = 0)</param>
        /// <returns>The number of flushed Submission Queue Entries</returns>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="minComplete"/> > <paramref name="toFlush"/></exception>
        /// <exception cref="SubmissionEntryDroppedException">If an invalid Submission Queue Entry was dropped</exception>
        /// <exception cref="ErrnoException">On negative result from syscall</exception>
        public uint Flush(uint toFlush, uint minComplete = 0)
            => _sq.Flush(_ringFd.DangerousGetHandle().ToInt32(), SubmissionPollingEnabled, toFlush, minComplete);
    }
}
