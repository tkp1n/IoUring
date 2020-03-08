using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using IoUring.Concurrent;
using Tmds.Linux;
using static Tmds.Linux.LibC;
using static IoUring.Internal.Helpers;
using static IoUring.Internal.ThrowHelper;

namespace IoUring.Internal
{
    internal sealed unsafe partial class SubmissionQueue
    {
        private const int ReadyForPrep = 0;
        private const int ReservedForPrep = 1;
        private const int ReadyForSubmit = 2;
        private const int ReservedForSubmit = 3;

        private readonly int[] _states;

        private SubmissionQueue(void* ringBase, io_sqring_offsets* offsets)
        {
            var entries = *Add<uint>(ringBase, offsets->ring_entries);
            _states = new int[entries];
        }

        /// <summary>
        /// Returns the number of entries in the Submission Queue.
        /// </summary>
        public uint Entries => _ringEntries;

        /// <summary>
        /// Determines the number of entries in the Submission Queue that can be used to prepare new submissions
        /// prior to the next <see cref="SubmitAndWait"/>.
        /// </summary>
        public uint EntriesToPrepare
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _ringEntries - EntriesToSubmit;
        }

        /// <summary>
        /// Calculates the number of prepared Submission Queue Entries that will be submitted to the kernel during
        /// the next <see cref="SubmitAndWait"/>.
        /// </summary>
        public uint EntriesToSubmit
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                uint head = _sqPolled ? Volatile.Read(ref *_head) : _headInternal;
                return unchecked(_tailInternal - head);
            }
        }

        internal io_uring_sqe* this[uint index] => &_sqes[index];

        private object Gate => this;

        public bool NextSubmissionQueueEntry(out Submission submission)
        {
            uint head;
            uint tailInternal;
            uint next;

            do
            {
                head = _sqPolled ? Volatile.Read(ref *_head) : Volatile.Read(ref _headInternal);
                tailInternal = _tailInternal;
                next = unchecked(tailInternal + 1);

                if (next - head > _ringEntries)
                {
                    submission = default;
                    return false;
                }
            } while (CompareExchange(ref _tailInternal, next, tailInternal) != tailInternal);

            uint idx = tailInternal & _ringMask;
            var sqeInternal = &_sqes[idx];

            Unsafe.InitBlockUnaligned(sqeInternal, 0x00, (uint) sizeof(io_uring_sqe));

            Debug.Assert(Interlocked.CompareExchange(ref _states[idx], ReservedForPrep, ReadyForPrep) == ReadyForPrep);

            submission = new Submission(sqeInternal, idx);
            return true;
        }

        public bool NextSubmissionQueueEntries(Span<Submission> submissions)
        {
            uint head;
            uint tailInternal;
            uint next;
            uint nofSubmissions = (uint) submissions.Length;

            do
            {
                head = _sqPolled ? Volatile.Read(ref *_head) : Volatile.Read(ref _headInternal);
                tailInternal = _tailInternal;
                next = unchecked(tailInternal + nofSubmissions);

                if (next - head > _ringEntries)
                {
                    return false;
                }
            } while (CompareExchange(ref _tailInternal, next, tailInternal) != tailInternal);

            for (int i = 0; i < nofSubmissions; i++)
            {
                uint idx = tailInternal++ & _ringMask;
                var sqeInternal = &_sqes[idx];

                Unsafe.InitBlockUnaligned(sqeInternal, 0x00, (uint) sizeof(io_uring_sqe));

                Debug.Assert(Interlocked.CompareExchange(ref _states[idx], ReservedForPrep, ReadyForPrep) == ReadyForPrep);

                submissions[i] = new Submission(sqeInternal, idx);
            }

            return true;
        }

        public void NotifyPrepared(uint idx)
        {
#if DEBUG
            Debug.Assert(Interlocked.CompareExchange(ref _states[idx], ReadyForSubmit, ReservedForPrep) == ReservedForPrep);
#else
            Volatile.Write(ref _states[idx], ReadyForSubmit);
#endif
        }

        private bool ShouldEnter(out uint enterFlags)
        {
            enterFlags = 0;
            if (!_sqPolled) return true;
            if ((*_flags & IORING_SQ_NEED_WAKEUP) != 0)
            {
                // Kernel is polling but transitioned to idle (IORING_SQ_NEED_WAKEUP)
                enterFlags |= IORING_ENTER_SQ_WAKEUP;
                return true;
            }

            // Kernel is still actively polling
            return false;
        }

        private uint Notify()
        {
            Debug.Assert(Monitor.IsEntered(Gate));

            uint tail = *_tail;
            uint tailInternal = _tailInternal;
            uint headInternal = _headInternal;
            if (headInternal == tailInternal)
            {
                return tail - *_head;
            }

            uint mask = _ringMask;
            uint* array = _array;
            uint toSubmit = unchecked(tailInternal - headInternal);
            uint idx;
            while (toSubmit-- != 0)
            {
                idx = headInternal & mask;

                Debug.Assert(_states[idx] == ReservedForPrep || _states[idx] == ReadyForSubmit);
                if (_states[idx] != ReadyForSubmit)
                {
                    // This is encountered when a producing thread reserved an SQE but did not finish preparing it for submission yet.
                    // Although there might be additional fully prepared SQEs further down the ring, we stop here and continue during the next invocation.
                    break;
                }
                _states[idx] = ReservedForSubmit;

                array[tail & mask] = idx;
                tail = unchecked(tail + 1);
                headInternal = unchecked(headInternal + 1);
            }

            // write barrier to ensure all manipulations above are visible to the kernel once the tail-bump is observed
            Volatile.Write(ref *_tail, tail);

            return tail - *_head;
        }

        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        private void CheckNoSubmissionsDropped() => Debug.Assert(Volatile.Read(ref *_dropped) == 0);

        private void UpdateInternals(uint submitted)
        {
            Debug.Assert(Monitor.IsEntered(Gate));

            uint headInternal = _headInternal;
            uint mask = _ringMask;
            uint idx;
            while (submitted-- != 0)
            {
                idx = headInternal & mask;
                Debug.Assert(_states[idx] == ReservedForSubmit);
                _states[idx] = ReadyForPrep;

                headInternal = unchecked(headInternal + 1);
            }

            _headInternal = headInternal;
        }

        public SubmitResult SubmitAndWait(int ringFd, uint minComplete, out uint operationsSubmitted)
        {
            lock (Gate)
            {
                uint toSubmit = Notify();

                if (!ShouldEnter(out uint enterFlags))
                {
                    CheckNoSubmissionsDropped();

                    // Assume all Entries are already known to the kernel via Notify above
                    goto SkipSyscall;
                }

                // For minComplete to take effect or if the kernel is polling for I/O, we must set IORING_ENTER_GETEVENTS
                if (minComplete > 0 || _ioPolled)
                {
                    enterFlags |= IORING_ENTER_GETEVENTS; // required for minComplete to take effect
                }
                else if (toSubmit == 0)
                {
                    // There are no submissions, we don't have to wait for completions and don't have to reap polled I/O completions
                    // --> We can skip the syscall and return directly.
                    goto SkipSyscall;
                }

                int res;
                int err = default;
                do
                {
                    res = io_uring_enter(ringFd, toSubmit, minComplete, enterFlags, (sigset_t*) NULL);
                } while (res == -1 && (err = errno) == EINTR);

                if (res == -1)
                {
                    if (err == EAGAIN || err == EBUSY)
                    {
                        operationsSubmitted = default;
                        return SubmitResult.AwaitCompletions;
                    }

                    ThrowErrnoException(res);
                }

                CheckNoSubmissionsDropped();

                uint submitted = (uint) res;
                UpdateInternals(submitted);

                return (operationsSubmitted = submitted) >= toSubmit ?
                    SubmitResult.SubmittedSuccessfully :
                    SubmitResult.SubmittedPartially;

            SkipSyscall:
                operationsSubmitted = toSubmit;
                return SubmitResult.SubmittedSuccessfully;
            }
        }
    }
}
