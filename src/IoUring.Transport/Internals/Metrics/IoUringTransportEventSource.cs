using System.Diagnostics.Tracing;

namespace IoUring.Transport.Internals.Metrics
{
    [EventSource(Name = "IoUring.Transport")]
    public sealed class IoUringTransportEventSource : EventSource
    {
        private readonly MeanAndVarianceCounter _completionsPerEnter;
        private readonly IncrementingCounter _blockingEnter;
        private readonly MeanAndVarianceCounter _submissionsPerEnter;
        private readonly IncrementingCounter _eventFdWrites;
        private readonly IncrementingCounter _syncFlush;
        private readonly IncrementingCounter _syncRead;

        public static readonly IoUringTransportEventSource Log = new IoUringTransportEventSource();

        public IoUringTransportEventSource()
        {
            _completionsPerEnter = new MeanAndVarianceCounter("Completions per io_uring_enter", this);
            _blockingEnter = new IncrementingCounter("io_uring_enter with min_complete = 1", this);
            _submissionsPerEnter = new MeanAndVarianceCounter("Submissions per io_uring_enter", this);
            _eventFdWrites = new IncrementingCounter("eventFd writes", this);
            _syncFlush = new IncrementingCounter("synchronous FlushAsync()", this);
            _syncRead = new IncrementingCounter("synchronous ReadAsync()", this);
        }

        public void ReportCompletionsPerEnter(int value) => _completionsPerEnter.ReportValue(value);

        public void ReportBlockingEnter() => _blockingEnter.Increment();

        public void ReportSubmissionsPerEnter(int value) => _submissionsPerEnter.ReportValue(value);

        public void ReportEventFdWrite() => _eventFdWrites.Increment();

        public void ReportSyncFlushAsync() => _syncFlush.Increment();

        public void ReportAsyncFlushAsync() => _syncFlush.Decrement();
        
        public void ReportSyncReadAsync() => _syncRead.Increment();

        public void ReportAsyncReadAsync() => _syncRead.Decrement();
    }
}