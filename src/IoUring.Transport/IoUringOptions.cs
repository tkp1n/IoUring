using System;
using System.IO.Pipelines;

namespace IoUring.Transport
{
    public class IoUringOptions
    {
        public int ThreadCount { get; set; } = Math.Min(Environment.ProcessorCount, 16);
        public PipeScheduler ApplicationSchedulingMode { get; set; } = PipeScheduler.ThreadPool;
    }
}