using System;
using Tmds.Linux;
using static Tmds.Linux.LibC;
using static IoUring.Internal.Helpers;
using static IoUring.Internal.ThrowHelper;
using System.Diagnostics;

namespace IoUring.Internal
{
    public abstract unsafe partial class BaseRing
    {
        /// <summary>
        /// Registers a set of buffers with the kernel to reduce per I/O overhead.
        /// </summary>
        /// <param name="iov">I/O vectors to register</param>
        /// <param name="iovcnt">Number of I/O vectors</param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="iovcnt"/> is negative</exception>
        /// <exception cref="ErrnoException">On failed syscall</exception>
        public void RegisterBuffers(iovec* iov, int iovcnt)
        {
            if (iovcnt < 0) ThrowArgumentOutOfRangeException(ExceptionArgument.iovcnt);
            Register(IORING_REGISTER_BUFFERS, iov, (uint) iovcnt);
        }

        /// <summary>
        /// Unregisters all buffers previously registered using <see cref="RegisterBuffers"/>.
        /// </summary>
        /// <exception cref="ErrnoException">On failed syscall</exception>
        public void UnregisterBuffers()
            => Unregister(IORING_UNREGISTER_BUFFERS);

        /// <summary>
        /// Registers a set of files with the kernel to reduce per I/O overhead.
        /// </summary>
        /// <param name="files">File descriptors to register</param>
        /// <param name="nrFiles">Number of file descriptors</param>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="nrFiles"/> is negative</exception>
        /// <exception cref="ErrnoException">On failed syscall</exception>
        public void RegisterFiles(int* files, int nrFiles)
        {
            if (nrFiles < 0) ThrowArgumentOutOfRangeException(ExceptionArgument.nrFiles);
            Register(IORING_REGISTER_FILES, files, (uint) nrFiles);
        }

        /// <summary>
        /// Unregisters all files previously registered using <see cref="RegisterFiles"/>.
        /// </summary>
        /// <exception cref="ErrnoException">On failed syscall</exception>
        public void UnregisterFiles()
            => Unregister(IORING_UNREGISTER_FILES);

        /// <summary>
        /// Updates the set of previously registered files.
        /// </summary>
        /// <param name="off">Offset within the original array at which the update shall occur</param>
        /// <param name="files">The update set of files</param>
        /// <param name="nrFiles">The number of files to update</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public int UpdateRegisteredFiles(uint off, int* files, int nrFiles)
        {
            if (nrFiles < 0) ThrowArgumentOutOfRangeException(ExceptionArgument.nrFiles);

            io_uring_files_update up;
            up.offset = off;
            up.fds = (ulong) files;

            return Register(IORING_REGISTER_FILES_UPDATE, &up, (uint) nrFiles);
        }

        /// <summary>
        /// It's possible to use eventfd(2) to get notified of completion events on an
        /// io_uring instance. If this is desired, an eventfd file descriptor can be
        /// registered through this operation.
        /// </summary>
        /// <param name="fd">Event file descriptor to register</param>
        public void RegisterEventFd(int fd)
            => Register(IORING_REGISTER_EVENTFD, &fd, 1);

        /// <summary>
        /// his works just like <see cref="RegisterEventFd"/>,
        /// except notifications are only posted for events that complete in an async
        /// manner. This means that events that complete inline while being submitted
        /// do not trigger a notification event.
        /// </summary>
        /// <param name="fd"></param>
        public void RegisterEventFdAsync(int fd)
            => Register(IORING_REGISTER_EVENTFD_ASYNC, &fd, 1);

        /// <summary>
        /// Unregisters the event file descriptor previously registered using <see cref="RegisterEventFd"/> or
        /// <see cref="RegisterEventFdAsync"/>.
        /// </summary>
        /// <exception cref="ErrnoException">On failed syscall</exception>
        public void UnregisterEventFd()
            => Unregister(IORING_UNREGISTER_EVENTFD);

        public ushort RegisterPersonality()
        {
            int ret = Register(IORING_REGISTER_PERSONALITY, NULL, 0);
            Debug.Assert(ret <= ushort.MaxValue, "Kernel only accepts __u16 as personality. We assume it only provides us with acceptable values");
            return (ushort) ret;
        }

        public int UnregisterPersonality(ushort id)
            => Register(IORING_UNREGISTER_PERSONALITY, NULL, id);

        private int Register(uint opcode, void* args, uint nrArgs)
        {
            int ret = io_uring_register(_ringFd.DangerousGetHandle().ToInt32(), opcode, args, nrArgs);
            if (ret < 0)
            {
                ThrowErrnoException();
            }

            return ret;
        }

        private void Unregister(uint opcode)
        {
            int ret = io_uring_register(_ringFd.DangerousGetHandle().ToInt32(), opcode, null, 0);
            if (ret < 0)
            {
                ThrowErrnoException();
            }
        }
    }
}