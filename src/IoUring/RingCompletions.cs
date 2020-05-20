using System;
using System.Runtime.CompilerServices;

namespace IoUring
{
    /// <summary>
    /// Provides access to an allocation-free enumerator over the currently available Completion Queue Events.
    /// </summary>
    public sealed class RingCompletions
    {
        private readonly Ring _ring;

        internal RingCompletions(Ring ring)
        {
            _ring = ring;
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(_ring);
        }

        public struct Enumerator : IDisposable
        {
            private readonly Ring _ring;
            private Completion _completion;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Enumerator(Ring ring)
            {
                _ring = ring;
                _completion = default;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool MoveNext() => _ring.TryReadEnumerator(out _completion);

            public Completion Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get => _completion;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose() => _ring.UpdateReadHeadPostEnumeration();
        }
    }
}