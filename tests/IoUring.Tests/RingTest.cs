using System;
using System.Threading;
using Xunit;

namespace IoUring.Tests
{
    public class RingTest
    {
        [Fact]
        public void SmokeTest()
        {
            var r = new Ring(8);

            Assert.Equal(8, r.SubmissionQueueSize);
            Assert.Equal(16, r.CompletionQueueSize);

            Assert.Equal(0, r.SubmissionEntriesUsed);
            Assert.Equal(8, r.SubmissionEntriesAvailable);

            Assert.NotNull(r);
            Assert.False(r.IoPollingEnabled);
            Assert.False(r.SubmissionPollingEnabled);
            Assert.False(r.SubmissionQueuePollingCpuAffinity);

            Assert.True(r.TryPrepareNop(123ul));

            Assert.Equal(1, r.SubmissionEntriesUsed);
            Assert.Equal(7, r.SubmissionEntriesAvailable);

            Assert.True(r.Submit(out var submitted));
            Assert.Equal(1u, submitted);

            Assert.Equal(0, r.SubmissionEntriesUsed);
            Assert.Equal(8, r.SubmissionEntriesAvailable);

            Assert.True(r.TryRead(out Completion c));
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

            Assert.True(r.Submit(out var submitted));
            Assert.Equal(i, submitted);

            for (uint j = 0; j < i; j++)
            {
                Assert.True(r.TryRead(out var c));
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

            Assert.True(r.Submit(out var submitted));
            Assert.Equal(i, submitted);

            Span<Completion> completions = stackalloc Completion[(int)i];

            r.Read(completions);
            for (uint j = 0; j < i; j++)
            {
                Assert.Equal(0, completions[(int)j].result);
                Assert.Equal(j, completions[(int)j].userData);
            }
        }

        [Fact]
        public void TryRead()
        {
            using var r = new Ring(8);

            uint i;
            for (i = 0; i < 8; i++)
            {
                Assert.True(r.TryPrepareNop(i));
                Assert.True(r.Submit(out var submitted));
                Assert.Equal(1u, submitted);

                Assert.True(r.TryRead(out var c));
                Assert.Equal(0, c.result);
                Assert.Equal(i, c.userData);
            }

            Assert.False(r.TryRead(out _));
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

            Assert.True(r.Submit(out var submitted));
            Assert.Equal(i, submitted);

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
            const int events = 1024;
            using var r = new Ring(events);

            var reader = new Thread(state =>
            {
                var ring = (Ring) state;
                ulong i = 0;
                while (i < events)
                {
                    while (i < events & ring.TryRead(out Completion c))
                    {
                        Assert.Equal(0, c.result);
                        Assert.Equal(i, c.userData);
                        i++;
                    }

                    if (i < events)
                    {
                        Completion c = ring.Read();
                        Assert.Equal(0, c.result);
                        Assert.Equal(i, c.userData);
                        i++;
                    }
                }
            });

            var writer = new Thread(state =>
            {
                var ring = (Ring) state;
                uint toSubmit = 0;
                for (ulong i = 0; i < events; i++)
                {
                    if (!ring.TryPrepareNop(i))
                    {
                        i--;
                        continue;
                    }

                    toSubmit++;

                    if (toSubmit >= 15)
                    {
                        Assert.True(ring.Submit(out var submitted));
                        toSubmit -= submitted;
                    }
                }

                while (toSubmit > 0)
                {
                    Assert.True(ring.Submit(out var submitted));
                    toSubmit -= submitted;
                }
            });

            reader.Start(r);
            writer.Start(r);

            reader.Join();
            writer.Join();
        }

        [Fact]
        public void DetectOverSubmit()
        {
            var r = new Ring(8);

            for (int i = 0; i < r.SubmissionQueueSize; i++)
            {
                r.PrepareNop();
            }

            Assert.Throws<SubmissionQueueFullException>(() =>
            {
                r.PrepareNop();
            });
            
            Assert.False(r.TryPrepareNop());
        }
    }
}

