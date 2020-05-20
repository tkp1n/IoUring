using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
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
                return unchecked(Volatile.Read(ref _tailInternal) - Volatile.Read(ref _headInternal));
            }
        }

        private object Gate => this;

        public bool NextSubmissionQueueEntry(out IoUring.Concurrent.Submission submission)
        {
            uint head;
            uint tailInternal;
            uint next;

            do
            {
                tailInternal = Volatile.Read(ref _tailInternal);
                head = _headInternal;
                next = unchecked(tailInternal + 1);

                if (head > tailInternal)
                {
                    // unfortunate interleaving occurred
                    tailInternal = UInt32.MaxValue; // ensure below CompareExchange fails and try again
                    continue;
                }

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

            submission = new IoUring.Concurrent.Submission(sqeInternal, idx);
            return true;
        }

        public bool NextSubmissionQueueEntries(Span<IoUring.Concurrent.Submission> submissions)
        {
            uint head;
            uint tailInternal;
            uint next;
            uint nofSubmissions = (uint) submissions.Length;

            do
            {
                tailInternal = Volatile.Read(ref _tailInternal);
                head = _headInternal;
                next = unchecked(tailInternal + nofSubmissions);

                if (head > tailInternal)
                {
                    // unfortunate interleaving occurred
                    tailInternal = UInt32.MaxValue; // ensure below CompareExchange fails and try again
                    continue;
                }

                if (next - head > _ringEntries)
                {
                    return false;
                }
            } while (CompareExchange(ref _tailInternal, next, tailInternal) != tailInternal);

            for (int i = 0; i < nofSubmissions; i++)
            {
                uint idx = tailInternal & _ringMask;
                tailInternal = unchecked(tailInternal + 1);
                var sqeInternal = &_sqes[idx];

                Unsafe.InitBlockUnaligned(sqeInternal, 0x00, (uint) sizeof(io_uring_sqe));

                Debug.Assert(Interlocked.CompareExchange(ref _states[idx], ReservedForPrep, ReadyForPrep) == ReadyForPrep);

                submissions[i] = new IoUring.Concurrent.Submission(sqeInternal, idx);
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

        private uint Notify()
        {
            Debug.Assert(Monitor.IsEntered(Gate));

            uint tailInternal = _tailInternal;
            uint headInternal = _headInternal;
            if (headInternal == tailInternal)
            {
                return 0;
            }

            uint tail = *_tail;
            uint mask = _ringMask;
            uint* array = _array;
            uint toSubmit = unchecked(tailInternal - headInternal);
            uint idx;
            int oldState;
            while (toSubmit-- != 0)
            {
                idx = headInternal & mask;
                oldState = _states[idx];
                if (oldState < ReadyForSubmit)
                {
                    // This is encountered when a producing thread did not finish preparing the SQE for submission yet.
                    // Although there might be additional fully prepared SQEs further down the ring, we stop here and continue during the next invocation.
                    break;
                }

#if DEBUG
                Debug.Assert(Interlocked.CompareExchange(ref _states[idx], ReservedForSubmit, oldState) == oldState);
#else
                _states[idx] = ReservedForSubmit;
#endif

                array[tail & mask] = idx;
                if (oldState != ReservedForSubmit)
                {
                     // Increment tail only once per transition to ReservedForSubmit
                    tail = unchecked(tail + 1);
                }

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
#if DEBUG
                Debug.Assert(Interlocked.CompareExchange(ref _states[idx], ReadyForPrep, ReservedForSubmit) == ReservedForSubmit);
#else
                _states[idx] = ReadyForPrep;
#endif
                headInternal = unchecked(headInternal + 1);
            }

            _headInternal = headInternal;
        }

        public SubmitResult SubmitAndWait(uint minComplete, out uint operationsSubmitted)
        {
            lock (Gate)
            {
                uint toSubmit = Notify();
                if (toSubmit == 0 && minComplete == 0)
                {
                    Debug.Assert(Volatile.Read(ref *_head) == _headInternal);

                    // There are no submissions, we don't have to wait for completions and don't have to reap polled I/O completions
                    // --> We can skip the syscall and return directly.
                    operationsSubmitted = default;
                    return SubmitResult.SubmittedSuccessfully;
                }

                // For minComplete to take effect, we must set IORING_ENTER_GETEVENTS
                uint enterFlags = minComplete > 0 ? IORING_ENTER_GETEVENTS : 0;

                int ringFd = _ringFd;
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
                        Debug.Assert(Volatile.Read(ref *_head) == _headInternal);

                        operationsSubmitted = default;
                        return SubmitResult.AwaitCompletions;
                    }

                    ThrowErrnoException(res);
                }

                CheckNoSubmissionsDropped();

                Debug.Assert(unchecked(Volatile.Read(ref *_head) - _headInternal) == res);

                uint submitted = (uint) res;
                UpdateInternals(submitted);

                return (operationsSubmitted = submitted) >= toSubmit ?
                    SubmitResult.SubmittedSuccessfully :
                    SubmitResult.SubmittedPartially;
            }
        }
    }
}
