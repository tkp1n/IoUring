using System;
using IoUring.Internal;

namespace IoUring
{
    public unsafe partial class Ring
    {
        private readonly uint _cqSize;

        private readonly CompletionQueue _cq;
        private readonly UnmapHandle _cqHandle;

        /// <summary>
        /// Checks whether a Completion Queue Event is available.
        /// </summary>
        /// <param name="result">The data from the observed Completion Queue Event if any</param>
        /// <returns>Whether a Completion Queue Event was observed</returns>
        public bool TryRead(ref Completion result) => _cq.TryRead(ref result);

        public void Read(Span<Completion> results)
            => _cq.Read(_ringFd.DangerousGetHandle().ToInt32(), results);
    }
}