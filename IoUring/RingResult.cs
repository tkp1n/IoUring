using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using IoUring.Internal;

namespace IoUring
{
    /// <summary>
    /// Re-usable and await-able result of an operation scheduled for execution via a <see cref="Ring"/>.
    /// </summary>
    public sealed class RingResult : ICriticalNotifyCompletion, IDisposable
    {
        private readonly PipeScheduler _ioScheduler;
        private readonly Exception _forcedException;
        private GCHandle _handle;
        private int _result;
        private Action _callback;

        private static readonly Action CallbackCompleted = () => { };
        private static readonly Action<object> InvokeStateAsAction = state => ((Action)state)();

        private RingResult(PipeScheduler ioScheduler)
        {
            if (ioScheduler == null) ThrowHelper.ThrowArgumentNullException(ThrowHelper.ExceptionArgument.ioScheduler);
            _ioScheduler = ioScheduler;
            _forcedException = null;
        }

        private RingResult(PipeScheduler ioScheduler, Exception exception)
        {
            if (ioScheduler == null) ThrowHelper.ThrowArgumentNullException(ThrowHelper.ExceptionArgument.ioScheduler);
            if (exception == null) ThrowHelper.ThrowArgumentNullException(ThrowHelper.ExceptionArgument.exception);

            _ioScheduler = ioScheduler;
            _forcedException = exception;
            _result = -1;
            _callback = CallbackCompleted;
        }

        /// <summary>
        /// Returns a handle to this (pinned) object.
        /// </summary>
        internal ulong Handle
            => (ulong) GCHandle.ToIntPtr(_handle);

        /// <summary>
        /// Creates a new instance of <see cref="RingResult"/>.
        /// </summary>
        /// <returns>A new instance of <see cref="RingResult"/></returns>
        internal static RingResult Create(PipeScheduler ioScheduler)
        {
            RingResult res = new RingResult(ioScheduler);
            res._handle = GCHandle.Alloc(res, GCHandleType.Weak);
            return res;
        }

        internal static RingResult CreateForException(PipeScheduler ioScheduler, Exception exception) => new RingResult(ioScheduler, exception);

        /// <summary>
        /// Returns the <see cref="RingResult"/> instance referred to by the given <paramref name="handle"/>.
        /// </summary>
        /// <param name="handle">Handle to an <see cref="RingResult"/> instance</param>
        /// <returns>The <see cref="RingResult"/> instance referred to by the given <paramref name="handle"/></returns>
        internal static RingResult TaskFromHandle(ulong handle)
        {
            if (handle == 0) ThrowHelper.ThrowArgumentOutOfRangeException(ThrowHelper.ExceptionArgument.handle);
            return (RingResult) GCHandle.FromIntPtr((IntPtr) handle).Target;
        }

        /// <summary>
        /// Get the awaiter for this instance; used as part of "await"
        /// </summary>
        public RingResult GetAwaiter()
            => this;

        /// <summary>
        /// Indicates whether the current operation is complete; used as part of "await"
        /// </summary>
        public bool IsCompleted => ReferenceEquals(_callback, CallbackCompleted);

        /// <summary>
        /// Gets the result of the async operation is complete; used as part of "await"
        /// </summary>
        public int GetResult()
        {
            Debug.Assert(ReferenceEquals(_callback, CallbackCompleted));

            _callback = null;

            if (_result < 0)
            {
                if (_forcedException != null) ThrowForcedException();
                ThrowHelper.ThrowErrnoException(-_result);
            }

            return _result;
        }

        /// <summary>
        /// Schedules a continuation for this operation; used as part of "await"
        /// </summary>
        public void OnCompleted(Action continuation)
        {
            if (continuation == null) ThrowHelper.ThrowArgumentNullException(ThrowHelper.ExceptionArgument.continuation);

            if (ReferenceEquals(Volatile.Read(ref _callback), CallbackCompleted)
                || ReferenceEquals(Interlocked.CompareExchange(ref _callback, continuation, null), CallbackCompleted))
            {
                _ioScheduler.Schedule(InvokeStateAsAction, continuation);
            }
        }

        /// <summary>
        /// Schedules a continuation for this operation; used as part of "await"
        /// </summary>
        public void UnsafeOnCompleted(Action continuation)
            => OnCompleted(continuation);

        /// <summary>
        /// Sets the result
        /// </summary>
        /// <param name="result"></param>
        internal void Complete(int result)
        {
            _result = result;
            Action continuation = Interlocked.Exchange(ref _callback, CallbackCompleted);

            if (continuation != null)
            {
                _ioScheduler.Schedule(InvokeStateAsAction, continuation);
            }
        }

        private void ThrowForcedException()
            => throw ForcedException;

        private Exception ForcedException
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get => _forcedException;
        }

        internal bool HasForcedException => _forcedException == null;

        /// <summary>
        /// Resets the result
        /// </summary>
        public void Reset() => _callback = null;

        public void Dispose()
        {
            if (_handle.IsAllocated)
            {
                _handle.Free();
            }
        }
    }
}