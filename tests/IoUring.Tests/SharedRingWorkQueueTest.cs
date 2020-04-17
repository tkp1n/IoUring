using Xunit;

namespace IoUring.Tests
{
    public class SharedRingWorkQueueTest 
    {
        [Fact]
        public void SmokeTest() 
        {
            Assert.True(SharedRingWorkQueue.IsSupported);

            var sharedWq = new SharedRingWorkQueue();

            using var r1 = sharedWq.Create(8);
            using var r2 = sharedWq.Create(8, new RingOptions
            {
                CompletionQueueSize = 64,
            });

            Assert.Equal(64, r2.CompletionQueueSize);
        }
    }
}