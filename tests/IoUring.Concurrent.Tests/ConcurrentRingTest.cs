using System;
using System.Threading;
using Xunit;
using static Tmds.Linux.LibC;

namespace IoUring.Concurrent.Tests
{
    public class ConcurrentRingTest
    {
        [Theory]
        [InlineData(1, 8)]
        [InlineData(4, 8)]
        [InlineData(1, 4096)]
        [InlineData(4, 4096)]
        public void SmokeTest(int threadCount, int ringSize)
        {
            var r = new ConcurrentRing(ringSize);
            var threads = new Thread[threadCount];
            var actionPerThread = ringSize / threadCount;
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(() =>
                {
                    for (int j = 0; j < actionPerThread; j++)
                    {
                        Assert.True(r.TryAcquireSubmission(out var submission));
                        submission.PrepareNop();
                        r.Release(submission);
                    }
                });
            }

            foreach (Thread t in threads)
            {
                t.Start();
            }

            foreach (Thread t in threads)
            {
                t.Join();
            }

            Assert.Equal(SubmitResult.SubmittedSuccessfully, r.SubmitAndWait((uint) ringSize, out var operationsSubmitted));
            Assert.Equal((uint) ringSize, operationsSubmitted);

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(() =>
                {
                    for (int j = 0; j < actionPerThread; j++)
                    {
                        Assert.True(r.TryRead(out var completion));
                        Assert.Equal(0, completion.result);
                    }
                });
            }

            foreach (Thread t in threads)
            {
                t.Start();
            }

            foreach (Thread t in threads)
            {
                t.Join();
            }
        }

        [Theory]
        [InlineData(1, 4, 16)]
        [InlineData(4, 4, 16)]
        [InlineData(1, 4, 4096)]
        [InlineData(4, 4, 4096)]
        public void SmokeTestMultiple(int threadCount, int batchSize, int ringSize)
        {
            var r = new ConcurrentRing(ringSize);
            var threads = new Thread[threadCount];
            var actionPerThread = ringSize / threadCount / batchSize;
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(() =>
                {
                    Span<Submission> submissions = stackalloc Submission[batchSize];
                    for (int j = 0; j < actionPerThread; j++)
                    {
                        Assert.True(r.TryAcquireSubmissions(submissions));
                        for (int k = 0; k < batchSize; k++)
                        {
                            submissions[k].PrepareNop();
                            r.Release(submissions[k]);
                        }
                    }
                });
            }

            foreach (Thread t in threads)
            {
                t.Start();
            }

            foreach (Thread t in threads)
            {
                t.Join();
            }

            Assert.Equal(SubmitResult.SubmittedSuccessfully, r.SubmitAndWait((uint) ringSize, out var operationsSubmitted));
            Assert.Equal((uint) ringSize, operationsSubmitted);

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(() =>
                {
                    for (int j = 0; j < actionPerThread * batchSize; j++)
                    {
                        Assert.True(r.TryRead(out var completion));
                        Assert.Equal(0, completion.result);
                    }
                });
            }

            foreach (Thread t in threads)
            {
                t.Start();
            }

            foreach (Thread t in threads)
            {
                t.Join();
            }
        }

        [Fact]
        public void SubmitIntermittedLinkedSubmissions()
        {
            var r = new ConcurrentRing(8);
            Span<Submission> submissions = stackalloc Submission[2];

            Assert.True(r.TryAcquireSubmissions(submissions));

            submissions[0].PrepareNop(options: SubmissionOption.Link);
            r.Release(submissions[0]);

            Assert.Equal(SubmitResult.SubmittedSuccessfully, r.Submit(out var submitted));
            Assert.Equal(1u, submitted);

            Assert.True(r.TryRead(out _));

            submissions[1].PrepareNop(options: SubmissionOption.Link);
            r.Release(submissions[1]);

            Assert.Equal(SubmitResult.SubmittedSuccessfully, r.Submit(out submitted));
            Assert.Equal(1u, submitted);

            Assert.True(r.TryRead(out _));
            Assert.False(r.TryRead(out _));
        }

        [Fact]
        public unsafe void SubmissionWithInvalidArgumentsDoesNotIncrementDropped()
        {
            var r = new ConcurrentRing(8);

            Assert.True(r.TryAcquireSubmission(out var submission));
            submission.PrepareNop(1u);
            r.Release(submission);

            Assert.True(r.TryAcquireSubmission(out submission));
            // prepare submission with invalid parameters
            submission.PrepareReadWrite(99, -1, (void*) IntPtr.Zero, -12, 0, 0, 2u, SubmissionOption.None);
            r.Release(submission);

            Assert.True(r.TryAcquireSubmission(out submission));
            submission.PrepareNop(3u);
            r.Release(submission);

            Assert.True(r.TryAcquireSubmission(out submission));
            submission.PrepareNop(4u);
            r.Release(submission);

            Assert.Equal(SubmitResult.SubmittedPartially, r.SubmitAndWait(3, out var submitted));
            Assert.Equal(2u, submitted);

            Assert.True(r.TryRead(out var c));
            Assert.Equal(1u, c.userData);

            Assert.True(r.TryRead(out c));
            Assert.Equal(2u, c.userData);
            Assert.Equal(EINVAL, -c.result);

            // Submissions after invalid one are ignored by kernel without dropped being incremented
            Assert.False(r.TryRead(out _));

            // Our ring size is 8 with 2 SQE still unsubmitted... we should only be able to prepare 6 additional SQEs
            for (uint i = 0; i < 6; i++)
            {
                Assert.True(r.TryAcquireSubmission(out submission));
                submission.PrepareNop(5 + i);
                r.Release(submission);
            }
            Assert.False(r.TryAcquireSubmission(out submission)); // This would overwrite the previously unsubmitted SQE

            Assert.Equal(SubmitResult.SubmittedSuccessfully, r.Submit(out submitted));
            Assert.Equal(8u, submitted);

            for (uint i = 0; i < 8; i++)
            {
                Assert.True(r.TryRead(out c));
                Assert.Equal(3 + i, c.userData);
            }
        }
    }
}
