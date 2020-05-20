using System;
using System.Threading;
using Xunit;
using static Tmds.Linux.LibC;

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

            Assert.Equal(SubmitResult.SubmittedSuccessfully, r.Submit(out var submitted));
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

            Assert.Equal(SubmitResult.SubmittedSuccessfully, r.Submit(out var submitted));
            Assert.Equal(i, submitted);

            for (uint j = 0; j < i; j++)
            {
                Assert.True(r.TryRead(out var c));
                Assert.Equal(0, c.result);
                Assert.Equal(j, c.userData);
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
                Assert.Equal(SubmitResult.SubmittedSuccessfully, r.Submit(out var submitted));
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

            Assert.Equal(SubmitResult.SubmittedSuccessfully, r.Submit(out var submitted));
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
                        Assert.Equal(SubmitResult.SubmittedSuccessfully, r.Submit(out var submitted));
                        toSubmit -= submitted;
                    }
                }

                while (toSubmit > 0)
                {
                    Assert.Equal(SubmitResult.SubmittedSuccessfully, r.Submit(out var submitted));
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

        [Fact]
        public unsafe void SubmissionWithInvalidArgumentsDoesNotIncrementDropped()
        {
            var r = new Ring(8);

            r.PrepareNop(1u);
            // prepare submission with invalid parameters
            Assert.True(r.TryPrepareReadWrite(99, -1, (void*) IntPtr.Zero, -12, 0, 0, 2u, SubmissionOption.None));
            r.PrepareNop(3u);

            Assert.Equal(SubmitResult.SubmittedPartially, r.SubmitAndWait(3, out var submitted));
            Assert.Equal(2u, submitted);

            Assert.True(r.TryRead(out var c));
            Assert.Equal(1u, c.userData);

            Assert.True(r.TryRead(out c));
            Assert.Equal(2u, c.userData);
            Assert.Equal(EINVAL, -c.result);

            // Submissions after invalid one are ignored by kernel without dropped being incremented
            Assert.False(r.TryRead(out _));

            // Our ring size is 8 with 1 SQE still unsubmitted... we should only be able to prepare 7 additional SQEs
            for (uint i = 0; i < 7; i++)
            {
                Assert.True(r.TryPrepareNop(4 + i));
            }
            Assert.False(r.TryPrepareNop(11u)); // This would overwrite the previously unsubmitted SQE

            Assert.Equal(SubmitResult.SubmittedSuccessfully, r.Submit(out submitted));
            Assert.Equal(8u, submitted);

            for (uint i = 0; i < 8; i++)
            {
                Assert.True(r.TryRead(out c));
                Assert.Equal(3 + i, c.userData);
            }
        }

        [Fact]
        public void UnsafeGetSubmissionReservesSqe()
        {
            using var ring = new Ring(8);

            Assert.True(ring.TryGetSubmissionQueueEntryUnsafe(out var reservedSqe));

            Assert.True(ring.TryPrepareNop(userData: 2));
            reservedSqe.PrepareNop(userData: 1);

            Assert.Equal(SubmitResult.SubmittedSuccessfully, ring.SubmitAndWait(2, out var submitted));
            Assert.Equal(2u, submitted);

            Assert.True(ring.TryRead(out var completion));
            Assert.Equal(0, completion.result);
            Assert.Equal(1u, completion.userData);

            Assert.True(ring.TryRead(out completion));
            Assert.Equal(0, completion.result);
            Assert.Equal(2u, completion.userData);

            Assert.False(ring.TryRead(out _));
        }

        [Fact]
        public void SkipDoesNotSubmitToKernel()
        {
            using var ring = new Ring(8);

            ReadWrite4Sqes(ring);

            Assert.True(ring.TryGetSubmissionQueueEntryUnsafe(out var reservedSqe));

            Assert.True(ring.TryPrepareNop(userData: 2));
            reservedSqe.PrepareNop(userData: 1);

            Assert.Equal(SubmitResult.SubmittedSuccessfully, ring.SubmitAndWait(1, 1, out var submitted));
            Assert.Equal(1u, submitted);

            Assert.True(ring.TryRead(out var completion));
            Assert.Equal(0, completion.result);
            Assert.Equal(2u, completion.userData);

            Assert.False(ring.TryRead(out _));

            ReadWrite4Sqes(ring);
        }

        private void ReadWrite4Sqes(Ring ring)
        {
            for (uint i = 1; i <= 4; i++)
            {
                Assert.True(ring.TryPrepareNop(userData: i));
            }

            Assert.Equal(SubmitResult.SubmittedSuccessfully, ring.SubmitAndWait(4, out var submitted));
            Assert.Equal(4u, submitted);

            for (uint i = 1; i <= 4; i++)
            {
                Assert.True(ring.TryRead(out var completion));
                Assert.Equal(0, completion.result);
                Assert.Equal(i, completion.userData);
            }

            Assert.False(ring.TryRead(out _));
        }
    }
}

