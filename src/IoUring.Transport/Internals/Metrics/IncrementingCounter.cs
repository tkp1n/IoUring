using System.Diagnostics.Tracing;
using System.Threading;

namespace IoUring.Transport.Internals.Metrics
{
    public class IncrementingCounter
    {
        private readonly PollingCounter _pollingCounter;
        private long _counter;

        public IncrementingCounter(string name, EventSource source)
        {
            _pollingCounter = new PollingCounter(name, source, Counter);
        }

        private double Counter() => _counter;

        public void Increment() => Interlocked.Increment(ref _counter);
    }
}