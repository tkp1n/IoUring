using System;
using Tmds.Linux;
using static Tmds.Linux.LibC;
using static IoUring.Internal.ThrowHelper;

namespace IoUring
{
    public sealed unsafe partial class Ring
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
            if (iovcnt < 0) throw new ArgumentOutOfRangeException(nameof(iovcnt), "must be non-negative");
            Register(IORING_REGISTER_FILES, iov, (uint) iovcnt);
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
            if (nrFiles < 0) throw new ArgumentOutOfRangeException(nameof(nrFiles), "must be non-negative");
            Register(IORING_REGISTER_FILES, files, (uint) nrFiles);
        }

        /// <summary>
        /// Unregisters all files previously registered using <see cref="RegisterFiles"/>.
        /// </summary>
        /// <exception cref="ErrnoException">On failed syscall</exception>
        public void UnregisterFiles()
            => Unregister(IORING_UNREGISTER_FILES);

        /// <summary>
        /// Registers an event with the kernel to reduce per I/O overhead.
        /// </summary>
        /// <param name="fd">Event file descriptor to register</param>
        public void RegisterEventFd(int fd)
            => Register(IORING_REGISTER_EVENTFD, &fd, 1);

        /// <summary>
        /// Unregisters the event file descriptor previously registered using <see cref="RegisterEventFd"/>.
        /// </summary>
        /// <exception cref="ErrnoException">On failed syscall</exception>
        public void UnregisterEventFd()
            => Unregister(IORING_UNREGISTER_EVENTFD);

        private void Register(uint opcode, void* args, uint nrArgs)
        {
            int ret = io_uring_register(_ringFd.DangerousGetHandle().ToInt32(), opcode, args, nrArgs);
            if (ret < 0)
            {
                ThrowErrnoException();
            }
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
