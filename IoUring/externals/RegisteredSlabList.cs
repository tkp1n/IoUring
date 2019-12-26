using System.Collections.Generic;
using IoUring;
using Tmds.Linux;

namespace System.Buffers
{
    internal class RegisteredSlabList
    {
        private readonly Ring _ring;
        private List<MemoryPoolSlab> _store;

        internal RegisteredSlabList(Ring ring)
        {
            _ring = ring;
            _store = new List<MemoryPoolSlab>();
        }

        internal void Push(MemoryPoolSlab slab)
        {
            lock (_ring)
            {
                _store.Add(slab);
                SyncRegisteredSlabs();
            }
        }

        internal bool TryPop(out MemoryPoolSlab slab)
        {
            lock (_ring)
            {
                int count = _store.Count;
                if (count == 0)
                {
                    slab = default;
                    return false;
                }

                slab = _store[count - 1];
                _store.RemoveAt(count - 1);

                SyncRegisteredSlabs();
                return true;
            }
        }

        private void SyncRegisteredSlabs()
        {
            var store = _store;
            Ring ring = _ring;
            unsafe
            {
                var iovs = stackalloc iovec[store.Count];
                int i = 0;
                foreach (MemoryPoolSlab currentSlab in store)
                {
                    ref iovec currentIov = ref iovs[i++];
                        
                    currentIov.iov_base = (void*) currentSlab.NativePointer;
                    currentIov.iov_len = currentSlab.Array.Length;
                }

                ring.UnregisterBuffers();
                ring.RegisterBuffers(iovs, store.Count);
            }
        }
    }
}