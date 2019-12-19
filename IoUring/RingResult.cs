using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using IoUring.Internal;

namespace IoUring
{
    public sealed class RingResult : ICriticalNotifyCompletion, IDisposable
    {
        private readonly PipeScheduler _ioScheduler;
        private GCHandle _handle;
        private int _result;
        private Action _callback;

        private static readonly Action CallbackCompleted = () => { };
        private static readonly Action<object> InvokeStateAsAction = state => ((Action)state)();

        private RingResult(PipeScheduler ioScheduler)
        {
            _ioScheduler = ioScheduler;
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
            var res = new RingResult(ioScheduler);
            res._handle = GCHandle.Alloc(res);
            return res;
        }

        /// <summary>
        /// Returns the <see cref="RingResult"/> instance referred to by the given <paramref name="handle"/>.
        /// </summary>
        /// <param name="handle">Handle to an <see cref="RingResult"/> instance</param>
        /// <returns>The <see cref="RingResult"/> instance referred to by the given <paramref name="handle"/></returns>
        internal static RingResult TaskFromHandle(ulong handle)
            => (RingResult) GCHandle.FromIntPtr((IntPtr) handle).Target;

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
                // this is the rare "kinda already complete" case; push to worker to prevent possible stack dive,
                // but prefer the custom scheduler when possible
                if (_ioScheduler == null)
                {
                    Task.Run(continuation);
                }
                else
                {
                    _ioScheduler.Schedule(InvokeStateAsAction, continuation);
                }
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
            var continuation = Interlocked.Exchange(ref _callback, CallbackCompleted);

            if (continuation != null)
            {
                _ioScheduler.Schedule(InvokeStateAsAction, continuation);
            }
        }

        public void Dispose() 
            => Dispose(true);

        private void Dispose(bool disposing)
        {
            if (_handle.IsAllocated)
                _handle.Free();
            
            if (disposing)
                GC.SuppressFinalize(this);
        }

        ~RingResult()
            => Dispose(false);
    }
}