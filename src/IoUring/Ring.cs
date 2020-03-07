using IoUring.Internal;

namespace IoUring
{
    public sealed partial class Ring : BaseRing
    {
        public Ring(int entries, RingOptions? options = null) : base(entries, options) { }
    }
}
