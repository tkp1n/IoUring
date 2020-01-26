using System;
using System.IO.Pipelines;
using System.Threading;
using Tmds.Linux;

namespace IoUring
{
    public sealed class RingHandler : IDisposable
    {
        private readonly Ring _ring;
        private readonly RingResultPool _pool;
        private readonly RingResult _ringFullException;
        private bool _terminate;

        public RingHandler(Ring ring, PipeScheduler ioScheduler)
        {
            _ring = ring;
            _pool = new RingResultPool(ioScheduler, _ring.CompletionQueueSize);
            _ringFullException = RingResult.CreateForException(ioScheduler, new SubmissionQueueFullException());

            new Thread(ReadLoop)
            {
                IsBackground = true
            }.Start();
        }

        public RingResult Nop(SubmissionOption options = SubmissionOption.None)
        {
            RingResult result = CreateRingResult();
            if (!_ring.TryPrepareNop(result.Handle, options))
            {
                _pool.Return(result);
                return _ringFullException;
            }

            return result;
        }

        public unsafe RingResult ReadV(int fd, iovec* iov, int count, off_t offset = default, int flags = 0,
            SubmissionOption options = SubmissionOption.None)
        {
            RingResult result = CreateRingResult();
            if (!_ring.TryPrepareReadV(fd, iov, count, offset, flags, result.Handle, options))
            {
                _pool.Return(result);
                return _ringFullException;
            }

            return result;
        }

        public unsafe RingResult WriteV(int fd, iovec* iov, int count, off_t offset = default, int flags = 0,
            SubmissionOption options = SubmissionOption.None)
        {
            RingResult result = CreateRingResult();
            if (!_ring.TryPrepareWriteV(fd, iov, count, offset, flags, result.Handle, options))
            {
                _pool.Return(result);
                return _ringFullException;
            }

            return result;
        }

        public RingResult Fsync(int fd, FsyncOption fsyncOptions = FsyncOption.FileIntegrity,
            SubmissionOption options = SubmissionOption.None)
        {
            RingResult result = CreateRingResult();
            if (!_ring.TryPrepareFsync(fd, fsyncOptions, result.Handle, options))
            {
                _pool.Return(result);
                return _ringFullException;
            }

            return result;
        }

        public unsafe RingResult Read(int fd, void* buf, size_t count, int index, off_t offset = default,
            SubmissionOption options = SubmissionOption.None)
        {
            RingResult result = CreateRingResult();
            if (!_ring.TryPrepareRead(fd, buf, count, index, offset, result.Handle, options))
            {
                _pool.Return(result);
                return _ringFullException;
            }

            return result;
        }

        public unsafe RingResult Write(int fd, void* buf, size_t count, int index, off_t offset = default,
            SubmissionOption options = SubmissionOption.None)
        {
            RingResult result = CreateRingResult();
            if (!_ring.TryPrepareWrite(fd, buf, count, index, offset, result.Handle, options))
            {
                _pool.Return(result);
                return _ringFullException;
            }

            return result;
        }

        public RingResult PollAdd(int fd, ushort pollEvents, SubmissionOption options = SubmissionOption.None)
        {
            RingResult result = CreateRingResult();
            if (!_ring.TryPreparePollAdd(fd, pollEvents, result.Handle, options))
            {
                _pool.Return(result);
                return _ringFullException;
            }

            return result;
        }

        public RingResult PollRemove(SubmissionOption options = SubmissionOption.None)
        {
            RingResult result = CreateRingResult();
            if (!_ring.TryPreparePollRemove(result.Handle, options))
            {
                _pool.Return(result);
                return _ringFullException;
            }

            return result;
        }

        public RingResult SyncFileRange(int fd, off_t offset, off_t count, uint flags,
            SubmissionOption options = SubmissionOption.None)
        {
            RingResult result = CreateRingResult();
            if (!_ring.TryPrepareSyncFileRange(fd, offset, count, flags, result.Handle, options))
            {
                _pool.Return(result);
                return _ringFullException;
            }

            return result;
        }

        public unsafe RingResult SendMsg(int fd, msghdr* msg, int flags,
            SubmissionOption options = SubmissionOption.None)
        {
            RingResult result = CreateRingResult();
            if (!_ring.TryPrepareSendMsg(fd, msg, flags, result.Handle, options))
            {
                _pool.Return(result);
                return _ringFullException;
            }

            return result;
        }

        public unsafe RingResult RecvMsg(int fd, msghdr* msg, int flags,
            SubmissionOption options = SubmissionOption.None)
        {
            RingResult result = CreateRingResult();
            if (!_ring.TryPrepareRecvMsg(fd, msg, flags, result.Handle, options))
            {
                _pool.Return(result);
                return _ringFullException;
            }

            return result;
        }

        public void Flush()
            => _ring.Flush(_ring.Submit());

        public void Return(RingResult result)
            => _pool.Return(result);

        private RingResult CreateRingResult()
            => _pool.Get();

        private void ReadLoop()
        {
            Completion completion = default;

            while (!_terminate)
            {
                if (!_ring.TryRead(ref completion))
                {
                    completion = _ring.Read();
                }

                try
                {
                    RingResult result = RingResult.TaskFromHandle(completion.userData);
                    result.Complete(completion.result);
                }
                catch (Exception)
                {
                    // swallow everything
                }
            }
        }

        public void Dispose()
        {
            Volatile.Write(ref _terminate, true);
            _ring?.Dispose();
        }
    }
}