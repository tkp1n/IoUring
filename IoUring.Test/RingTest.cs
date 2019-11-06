using System;
using Xunit;

namespace IoUring.Test
{
    public class RingTest
    {
        [Fact]
        public void SmokeTest() 
        {
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
            Assert.Equal(0, c.result);
            Assert.Equal(123ul, c.userData);
        }

        [Fact]
        public void BulkSubmit()
        {
            using var r = new Ring(8);

            uint i;
            for (i = 0; i < 8; i++)
            {
                Assert.True(r.PrepareNop(i));
            }

            Assert.Equal(i, r.Submit());
            Assert.Equal(i, r.Flush(i));

            Completion c = default;
            for (uint j = 0; j < i; j++)
            {
                Assert.True(r.TryRead(ref c));
                Assert.Equal(0, c.result);
                Assert.Equal(j, c.userData);
            }
        }

        [Fact]
        public void BulkRead()
        {
            using var r = new Ring(8);
            
            uint i;
            for (i = 0; i < 8; i++)
            {
                Assert.True(r.PrepareNop(i));
            }

            Assert.Equal(i, r.Submit());
            Assert.Equal(i, r.Flush(i));

            Span<Completion> completions = stackalloc Completion[(int)i];

            r.Read(completions);
            for (uint j = 0; j < i; j++)
            {
                Assert.Equal(0, completions[(int)j].result);
                Assert.Equal(j, completions[(int)j].userData);
            }
        }
    }
}

