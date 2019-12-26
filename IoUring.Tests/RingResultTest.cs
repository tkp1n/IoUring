using System;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Xunit;

namespace IoUring.Tests
{
    public class RingResultTest
    {
        [Fact]
        public async void BasicCase()
        {
            var result = RingResult.Create(PipeScheduler.ThreadPool);

            result.Complete(1);

            Assert.Equal(1, await result);
        }

        [Fact]
        public async void AdvancedCase()
        {
            var result = RingResult.Create(PipeScheduler.ThreadPool);

            var tasks = new[]
            {
                Task.Run(async () => await result),
                Task.Run(() => result.Complete(2))
            };

            await Task.WhenAll(tasks);

            Assert.Equal(2, ((Task<int>)tasks[0]).Result);
        }

        [Fact]
        public async void ReUse()
        {
            var result = RingResult.Create(PipeScheduler.ThreadPool);

            result.Complete(3);

            Assert.True(result.IsCompleted);

            Assert.Equal(3, await result);

            Assert.False(result.IsCompleted);

            result.Complete(4);

            Assert.Equal(4, await result);
        }

        [Fact]
        public void RecreateFromHandle()
        {
            var result = RingResult.Create(PipeScheduler.ThreadPool);

            var handle = result.Handle;

            var result2 = RingResult.TaskFromHandle(handle);

            Assert.Equal(result, result2);
        }

        [Fact]
        public async void ForException()
        {
            var ex = new Exception();
            var result = RingResult.CreateForException(PipeScheduler.ThreadPool, ex);

            Assert.Equal(ex, await Assert.ThrowsAsync<Exception>(async () => await result));
            result.Dispose();
        }

        [Fact]
        public async void ForExceptionThrows()
        {
            var ex = new Exception();
            var result = RingResult.CreateForException(PipeScheduler.ThreadPool, ex);

            var e = await Assert.ThrowsAsync<Exception>(async () => await result);
            Assert.Equal(ex, e);
        }

        [Fact]
        public async void ThrowsErrnoException()
        {
            var result = RingResult.Create(PipeScheduler.ThreadPool);

            result.Complete(-1);

            var e = await Assert.ThrowsAsync<ErrnoException>(async () => await result);
            Assert.Equal(1, e.Errno);
        }
    }
}