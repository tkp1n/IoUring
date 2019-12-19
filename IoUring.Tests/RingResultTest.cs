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
    }
}