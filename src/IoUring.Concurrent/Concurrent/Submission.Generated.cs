using Tmds.Linux;
using static Tmds.Linux.LibC;

namespace IoUring.Concurrent
{
    public readonly unsafe partial struct Submission
    {
        /// <summary>
        /// Prepares this Submission Queue Entry as a NOP.
        /// </summary>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PrepareNop(ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_NOP;
                sqe->flags = (byte) options;
                sqe->fd = -1;
                sqe->user_data = userData;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as a readv, preadv or preadv2.
        /// </summary>
        /// <param name="fd">File descriptor to read from</param>
        /// <param name="iov">I/O vectors to read to</param>
        /// <param name="count">Number of I/O vectors</param>
        /// <param name="offset">Offset in bytes into the I/O vectors (as per preadv)</param>
        /// <param name="flags">Flags for the I/O (as per preadv2)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PrepareReadV(int fd, iovec* iov, int count, off_t offset = default, int flags = 0, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_READV;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->off = (ulong) (long) offset;
                sqe->addr = (ulong) iov;
                sqe->len = (uint) count;
                sqe->rw_flags = flags;
                sqe->user_data = userData;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as a writev, pwritev or pwritev2.
        /// </summary>
        /// <param name="fd">File descriptor to write to</param>
        /// <param name="iov">I/O vectors to write</param>
        /// <param name="count">Number of I/O vectors</param>
        /// <param name="offset">Offset in bytes into the I/O vectors (as per pwritev)</param>
        /// <param name="flags">Flags for the I/O (as per pwritev2)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PrepareWriteV(int fd, iovec* iov, int count, off_t offset = default, int flags = 0, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_WRITEV;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->off = (ulong) (long) offset;
                sqe->addr = (ulong) iov;
                sqe->len = (uint) count;
                sqe->rw_flags = flags;
                sqe->user_data = userData;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as a fsync.
        /// </summary>
        /// <param name="fd">File descriptor to synchronize</param>
        /// <param name="fsyncOptions">Integrity options</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PrepareFsync(int fd, FsyncOption fsyncOptions = FsyncOption.FileIntegrity, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_FSYNC;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->fsync_flags = (uint) fsyncOptions;
                sqe->user_data = userData;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as a read using a registered buffer/file.
        /// </summary>
        /// <param name="fd">File descriptor to read from</param>
        /// <param name="buf">Buffer/file to read to</param>
        /// <param name="count">Number of bytes to read</param>
        /// <param name="index">Index of buffer/file</param>
        /// <param name="offset">Offset in bytes into the file descriptor (as per preadv)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PrepareReadFixed(int fd, void* buf, size_t count, int index, off_t offset = default, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_READ_FIXED;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->off = (ulong) (long) offset;
                sqe->addr = (ulong) buf;
                sqe->len = (uint) count;
                sqe->user_data = userData;
                sqe->buf_index = (ushort) index;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as a write using a registered buffer/file.
        /// </summary>
        /// <param name="fd">File descriptor to write to</param>
        /// <param name="buf">Buffer/file to write</param>
        /// <param name="count">Number of bytes to write</param>
        /// <param name="index">Index of buffer/file</param>
        /// <param name="offset">Offset in bytes into the file descriptor (as per pwritev)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PrepareWriteFixed(int fd, void* buf, size_t count, int index, off_t offset = default, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_WRITE_FIXED;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->off = (ulong) (long) offset;
                sqe->addr = (ulong) buf;
                sqe->len = (uint) count;
                sqe->user_data = userData;
                sqe->buf_index = (ushort) index;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as a one-shot poll of the file descriptor.
        /// </summary>
        /// <param name="fd">File descriptor to poll</param>
        /// <param name="pollEvents">Events to poll for</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PreparePollAdd(int fd, ushort pollEvents, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_POLL_ADD;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->poll_events = pollEvents;
                sqe->user_data = userData;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as a request for removal of a previously added poll request.
        /// </summary>
        /// <param name="pollUserData">userData of the poll submission that should be removed</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PreparePollRemove(ulong pollUserData, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_POLL_REMOVE;
                sqe->flags = (byte) options;
                sqe->fd = -1;
                sqe->addr = pollUserData;
                sqe->user_data = userData;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as a sync_file_range.
        /// </summary>
        /// <param name="fd">File descriptor to sync</param>
        /// <param name="offset">Offset in bytes into the file</param>
        /// <param name="count">Number of bytes to sync</param>
        /// <param name="flags">Flags for the operation (as per sync_file_range)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PrepareSyncFileRange(int fd, off_t offset, off_t count, uint flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_SYNC_FILE_RANGE;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->off = (ulong) (long) offset;
                sqe->len = (uint) count;
                sqe->sync_range_flags = flags;
                sqe->user_data = userData;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as a sendmsg.
        /// </summary>
        /// <param name="fd">File descriptor to send to</param>
        /// <param name="msg">Message to send</param>
        /// <param name="flags">Flags for the operation (as per sendmsg)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PrepareSendMsg(int fd, msghdr* msg, uint flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_SENDMSG;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->addr = (ulong) msg;
                sqe->len = 1;
                sqe->msg_flags = flags;
                sqe->user_data = userData;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as a recvmsg.
        /// </summary>
        /// <param name="fd">File descriptor to receive from</param>
        /// <param name="msg">Message to read to</param>
        /// <param name="flags">Flags for the operation (as per recvmsg)</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PrepareRecvMsg(int fd, msghdr* msg, uint flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_RECVMSG;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->addr = (ulong) msg;
                sqe->len = 1;
                sqe->msg_flags = flags;
                sqe->user_data = userData;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as a timeout.
        /// </summary>
        /// <param name="ts">The amount of time after which the timeout should trigger if less than <paramref name="count"/> submissions completed.</param>
        /// <param name="count">The amount of completed submissions after which the timeout should trigger</param>
        /// <param name="timeoutOptions">Options on how <paramref name="ts"/> is interpreted</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PrepareTimeout(timespec* ts, uint count = 1, TimeoutOptions timeoutOptions = TimeoutOptions.Relative, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_TIMEOUT;
                sqe->flags = (byte) options;
                sqe->fd = -1;
                sqe->off = count;
                sqe->addr = (ulong) ts;
                sqe->len = 1;
                sqe->timeout_flags = (uint) timeoutOptions;
                sqe->user_data = userData;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as the removal of a timeout.
        /// </summary>
        /// <param name="timeoutUserData">userData of the timeout submission that should be removed</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PrepareTimeoutRemove(ulong timeoutUserData, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_TIMEOUT_REMOVE;
                sqe->flags = (byte) options;
                sqe->fd = -1;
                sqe->addr = timeoutUserData;
                sqe->user_data = userData;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as an accept.
        /// </summary>
        /// <param name="fd">File descriptor to accept from</param>
        /// <param name="addr">(out) the address of the connected client.</param>
        /// <param name="addrLen">(out) the length of the address</param>
        /// <param name="flags">Flags as per accept4</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PrepareAccept(int fd, sockaddr* addr, socklen_t* addrLen, int flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_ACCEPT;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->off = (ulong) addrLen;
                sqe->addr = (ulong) addr;
                sqe->accept_flags = (uint) flags;
                sqe->user_data = userData;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as the cancellation of a previously submitted item.
        /// </summary>
        /// <param name="opUserData">userData of the operation to cancel</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PrepareCancel(ulong opUserData, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_ASYNC_CANCEL;
                sqe->flags = (byte) options;
                sqe->fd = -1;
                sqe->addr = opUserData;
                sqe->user_data = userData;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as a timeout to a previously prepared linked item.
        /// </summary>
        /// <param name="ts">The amount of time after which the timeout should trigger</param>
        /// <param name="timeoutOptions">Options on how <paramref name="ts"/> is interpreted</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PrepareLinkTimeout(timespec* ts, TimeoutOptions timeoutOptions = TimeoutOptions.Relative, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_LINK_TIMEOUT;
                sqe->flags = (byte) options;
                sqe->fd = -1;
                sqe->addr = (ulong) ts;
                sqe->len = 1;
                sqe->timeout_flags = (uint) timeoutOptions;
                sqe->user_data = userData;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as a connect.
        /// </summary>
        /// <param name="fd">The socket to connect on</param>
        /// <param name="addr">The address to connect to</param>
        /// <param name="addrLen">The length of the address</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PrepareConnect(int fd, sockaddr* addr, socklen_t addrLen, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_CONNECT;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->off = addrLen;
                sqe->addr = (ulong) addr;
                sqe->user_data = userData;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as a fallocate.
        /// </summary>
        /// <param name="fd">The file to manipulate the allocated disk space for</param>
        /// <param name="mode">The operation to be performed</param>
        /// <param name="offset">Offset in bytes into the file</param>
        /// <param name="len">Number of bytes to operate on</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PrepareFallocate(int fd, int mode, off_t offset, off_t len, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_FALLOCATE;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->off = (ulong) (long) offset;
                sqe->addr = (ulong) (long) len;
                sqe->len = (uint) mode;
                sqe->user_data = userData;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as a closeat.
        /// </summary>
        /// <param name="dfd">Directory file descriptor</param>
        /// <param name="path">Path to be opened</param>
        /// <param name="flags">Flags for the open operation (e.g. access mode)</param>
        /// <param name="mode">File mode bits</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PrepareOpenAt(int dfd, byte* path, int flags, mode_t mode = default, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_OPENAT;
                sqe->flags = (byte) options;
                sqe->fd = dfd;
                sqe->addr = (ulong) path;
                sqe->len = (uint) mode;
                sqe->user_data = userData;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as a close.
        /// </summary>
        /// <param name="fd">File descriptor to close</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PrepareClose(int fd, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_CLOSE;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->user_data = userData;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as an update to the registered files.
        /// </summary>
        /// <param name="fds">File descriptors to add / -1 to remove</param>
        /// <param name="nrFds">Number of changing file descriptors</param>
        /// <param name="offset">Offset into the previously registered files</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PrepareFilesUpdate(int* fds, int nrFds, int offset, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_CLOSE;
                sqe->flags = (byte) options;
                sqe->fd = -1;
                sqe->off = (ulong) (long) offset;
                sqe->addr = (ulong) fds;
                sqe->len = (uint) nrFds;
                sqe->user_data = userData;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as a statx.
        /// </summary>
        /// <param name="dfd">Directory file descriptor for relative paths</param>
        /// <param name="path">Absolute or relative path</param>
        /// <param name="flags">Influence pathname-based lookup</param>
        /// <param name="mask">Identifies the required fields</param>
        /// <param name="statxbuf">Buffer for the required information</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PrepareStatx(int dfd, byte* path, int flags, uint mask, statx* statxbuf, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_STATX;
                sqe->flags = (byte) options;
                sqe->fd = dfd;
                sqe->off = (ulong) statxbuf;
                sqe->addr = (ulong) path;
                sqe->len = mask;
                sqe->user_data = userData;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as a read.
        /// </summary>
        /// <param name="fd">File descriptor</param>
        /// <param name="buf">Buffer to read to</param>
        /// <param name="nbytes">Number of bytes to read</param>
        /// <param name="offset">File offset to read at</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PrepareRead(int fd, void* buf, uint nbytes, off_t offset, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_READ;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->off = (ulong) (long) offset;
                sqe->addr = (ulong) buf;
                sqe->len = nbytes;
                sqe->user_data = userData;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as a write.
        /// </summary>
        /// <param name="fd">File descriptor</param>
        /// <param name="buf">Buffer to write</param>
        /// <param name="nbytes">Number of bytes to write</param>
        /// <param name="offset">File offset to write at</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PrepareWrite(int fd, void* buf, uint nbytes, off_t offset, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_WRITE;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->off = (ulong) (long) offset;
                sqe->addr = (ulong) buf;
                sqe->len = nbytes;
                sqe->user_data = userData;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as a posix_fadvise.
        /// </summary>
        /// <param name="fd">File descriptor</param>
        /// <param name="offset">Offset into the file</param>
        /// <param name="len">Length of the file range</param>
        /// <param name="advice">Advice for the file range</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PrepareFadvise(int fd, off_t offset, off_t len, int advice, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_FADVISE;
                sqe->flags = (byte) options;
                sqe->fd = fd;
                sqe->off = (ulong) (long) offset;
                sqe->len = (uint) len;
                sqe->user_data = userData;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as an madvise.
        /// </summary>
        /// <param name="addr">Start of address range</param>
        /// <param name="len">Length of address range</param>
        /// <param name="advice">Advice for address range</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PrepareMadvise(void* addr, off_t len, int advice, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_MADVISE;
                sqe->flags = (byte) options;
                sqe->fd = -1;
                sqe->len = (uint) len;
                sqe->user_data = userData;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as a send.
        /// </summary>
        /// <param name="sockfd">Socket file descriptor</param>
        /// <param name="buf">Buffer to send</param>
        /// <param name="len">Length of buffer to send</param>
        /// <param name="flags">Flags for the send</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PrepareSend(int sockfd, void* buf, size_t len, int flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_SEND;
                sqe->flags = (byte) options;
                sqe->fd = sockfd;
                sqe->addr = (ulong) buf;
                sqe->len = (uint) len;
                sqe->msg_flags = (uint) flags;
                sqe->user_data = userData;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as a recv.
        /// </summary>
        /// <param name="sockfd">Socket file descriptor</param>
        /// <param name="buf">Buffer to read to</param>
        /// <param name="len">Length of buffer to read to</param>
        /// <param name="flags">Flags for the recv</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PrepareRecv(int sockfd, void* buf, size_t len, int flags, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_RECV;
                sqe->flags = (byte) options;
                sqe->fd = sockfd;
                sqe->addr = (ulong) buf;
                sqe->len = (uint) len;
                sqe->msg_flags = (uint) flags;
                sqe->user_data = userData;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as an openat2.
        /// </summary>
        /// <param name="dfd">Directory file descriptor</param>
        /// <param name="path">Path to be opened</param>
        /// <param name="how">How pat should be opened</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PrepareOpenAt2(int dfd, byte* path, open_how* how, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_OPENAT2;
                sqe->flags = (byte) options;
                sqe->fd = dfd;
                sqe->off = (ulong) how;
                sqe->addr = (ulong) path;
                sqe->len = SizeOf.open_how;
                sqe->user_data = userData;
                sqe->personality = personality;
            }
        }

        /// <summary>
        /// Prepares this Submission Queue Entry as an epoll_ctl.
        /// </summary>
        /// <param name="epfd">epoll instance file descriptor</param>
        /// <param name="fd">File descriptor</param>
        /// <param name="op">Operation to be performed for the file descriptor</param>
        /// <param name="ev">Settings</param>
        /// <param name="userData">User data that will be returned with the respective <see cref="Completion"/></param>
        /// <param name="options">Options for the handling of the prepared Submission Queue Entry</param>
        /// <param name="personality">The personality to impersonate for this submission</param>
        public void PrepareEpollCtl(int epfd, int fd, int op, epoll_event* ev, ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
        {
            var sqe = _sqe;

            unchecked
            {
                sqe->opcode = IORING_OP_EPOLL_CTL;
                sqe->flags = (byte) options;
                sqe->fd = epfd;
                sqe->off = (ulong) fd;
                sqe->addr = (ulong) ev;
                sqe->len = (uint) op;
                sqe->user_data = userData;
                sqe->personality = personality;
            }
        }

    }
}
