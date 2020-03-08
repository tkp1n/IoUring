using System;
using System.Threading;
using Xunit;

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
    }
}
