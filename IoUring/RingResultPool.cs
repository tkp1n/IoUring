using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;

namespace IoUring
{
    /// <summary>
    /// Object-pool for <see cref="RingResult"/> based on Microsoft.Extensions.ObjectPool
    /// </summary>
    /// <remarks>This implementation keeps a cache of retained objects. This means that if objects are returned when the pool has already reached "maximumRetained" objects they will be available to be Garbage Collected.</remarks>
    public sealed class RingResultPool
    {
        private readonly PipeScheduler _ioScheduler;
        private readonly RingResult[] _items;
        private RingResult _firstItem;

        /// <summary>
        /// Creates an instance of <see cref="RingResultPool"/>.
        /// </summary>
        /// <param name="ioScheduler">Scheduler to be used to run continuations on <see cref="RingResult"/>s</param>
        /// <param name="maximumRetained">The maximum number of objects to retain in the pool. Defaults to the maximum number of pending Completion Queue Events (8192).</param>
        public RingResultPool(PipeScheduler ioScheduler, int maximumRetained = 4096 * 2)
        {
            _ioScheduler = ioScheduler;
            // -1 due to _firstItem
            _items = new RingResult[maximumRetained - 1];
        }

        public RingResult Get()
        {
            RingResult item = _firstItem;
            if (item == null || Interlocked.CompareExchange(ref _firstItem, null, item) != item)
            {
                var items = _items;
                for (int i = 0; i < items.Length; i++)
                {
                    item = items[i];
                    if (item != null && Interlocked.CompareExchange(ref items[i], null, item) == item)
                    {
                        return item;
                    }
                }

                item = Create();
            }

            return item;
        }

        public void Return(RingResult obj)
        {
            if (obj.HasForcedException) return;
            if (obj.IsCompleted) obj.Reset();

            if (_firstItem != null || Interlocked.CompareExchange(ref _firstItem, obj, null) != null)
            {
                var items = _items;
                for (int i = 0; i < items.Length && Interlocked.CompareExchange(ref items[i], obj, null) != null; ++i)
                {
                }
            }
        }

        // Non-inline to improve its code quality as uncommon path
        [MethodImpl(MethodImplOptions.NoInlining)]
        private RingResult Create() => RingResult.Create(_ioScheduler);
    }
}