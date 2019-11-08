using System;
using System.Threading;
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
            Assert.False(r.IoPollingEnabled);
            Assert.False(r.SubmissionPollingEnabled);
            Assert.False(r.SubmissionQueuePollingCpuAffinity);

            Assert.True(r.TryPrepareNop(123ul));
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
                Assert.True(r.TryPrepareNop(i));
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
                Assert.True(r.TryPrepareNop(i));
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

        [Fact]
        public void BlockingSingleRead()
        {
            using var r = new Ring(8);

            uint i;
            for (i = 0; i < 8; i++)
            {
                Assert.True(r.TryPrepareNop(i));
            }

            Assert.Equal(i, r.Submit());
            Assert.Equal(i, r.Flush(i));

            for (uint j = 0; j < i; j++)
            {
                var completion = r.Read();
                Assert.Equal(0, completion.result);
                Assert.Equal(j, completion.userData);
            }
        }

        [Fact]
        public void ParallelReadWrite()
        {
            using var r = new Ring(1024);

            var reader = new Thread(state =>
            {
                var ring = (Ring) state;
                Completion c = default;
                ulong i = 0;
                while (i < 10_000)
                {
                    while (i < 10_000 & ring.TryRead(ref c))
                    {
                        i++;
                        Assert.Equal(0, c.result);
                        Assert.Equal(i, c.userData);
                    }

                    if (i < 10_000)
                    {
                        c = ring.Read();
                        i++;
                        Assert.Equal(0, c.result);
                        Assert.Equal(i, c.userData);
                    }
                }
            });

            var writer = new Thread(state =>
            {
                var ring = (Ring) state;
                uint toSubmit = 0;
                uint toFlush = 0;
                for (ulong i = 0; i < 10_000; i++)
                {
                    if (!ring.TryPrepareNop(i))
                    {
                        Thread.Sleep(10);
                        i--;
                        continue;
                    }

                    toSubmit++;
                    if (toSubmit % 10 == 0)
                    {
                        toSubmit = 0;
                        toFlush += ring.Submit();
                    }

                    if (toFlush % 30 == 0)
                    {
                        ring.Flush(toFlush);
                        toFlush = 0;
                    }
                }
            });

            reader.Start(r);
            writer.Start(r);

            reader.Join();
            writer.Join();
        }
    }
}

