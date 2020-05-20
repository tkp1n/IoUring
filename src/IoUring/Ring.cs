using System.Runtime.CompilerServices;

namespace IoUring
{
    public sealed partial class Ring
    {
        private readonly uint _cqSize;
        private uint _operationsInFlight;

        public Ring(int entries, RingOptions? options = null) : this(entries, null, options)
        {
            _cqSize = SupportsNoDrop ? uint.MaxValue : _cq.Entries;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool CheckAndIncrementOperationsInFlight()
        {
            var ops = _operationsInFlight;
            ops++;
            if (ops > _cqSize) return false;
            _operationsInFlight = ops;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DecrementOperationsInFlight()
        {
            _operationsInFlight--;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void DecrementOperationsInFlight(int value)
        {
            _operationsInFlight -= (uint) value;
        }
    }
}
