using Xunit;

namespace IoUring.Test
{
    public class RingTest
    {
        [Fact]
        public void SmokeTest() {
            var r = new Ring(8);
            Assert.NotNull(r);
            Assert.False(r.KernelIoPolling);
            Assert.False(r.KernelSubmissionQueuePolling);
            Assert.False(r.PollingThreadCpuAffinity);

            Assert.True(r.PrepareNop(123ul));
            Assert.Equal(1u, r.Submit());
            Assert.Equal(1u, r.Flush(1));

            Completion c = default;
            Assert.True(r.TryRead(ref c));
            Assert.Equal(123ul, c.userData);
            Assert.Equal(0, c.res);
        }
    }
}

