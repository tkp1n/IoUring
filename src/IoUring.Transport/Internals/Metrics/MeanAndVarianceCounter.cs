using System.Diagnostics.Tracing;

namespace IoUring.Transport.Internals.Metrics
{
    public class MeanAndVarianceCounter
    {
        private PollingCounter _meanCounter;
        private PollingCounter _varianceCounter;

        private long _count;
        private double _mean;
        private double _m2;

        public MeanAndVarianceCounter(string name, EventSource source)
        {
            _meanCounter = new PollingCounter($"{name} (mean)", source, Mean);
            _varianceCounter = new PollingCounter($"{name} (variance)", source, Variance);
        }

        private double Mean() => _count < 2 ? double.NaN : _mean;

        private double Variance() => _count < 2 ? double.NaN : _m2 / _count;

        public void ReportValue(int val)
        {
            lock (this)
            {
                var (count, mean, m2) = (_count, _mean, _m2);
                count++;
                var delta = val - mean;
                mean += delta / count;
                var delta2 = val - mean;
                m2 += delta * delta2;

                (_count, _mean, _m2) = (count, mean, m2);                
            }
        }
    }
}