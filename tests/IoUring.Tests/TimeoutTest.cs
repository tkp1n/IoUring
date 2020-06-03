using System;
using System.Threading;
using Tmds.Linux;
using Xunit;
using static Tmds.Linux.LibC;

namespace IoUring.Tests
{
    public class TimeoutTest
    {
        [Fact]
        public unsafe void TimeoutFiresAfterTimeSpecified()
        {
            var r = new Ring(8);

            timespec ts;
            ts.tv_sec = 1;
            ts.tv_nsec = 0;

            Assert.True(r.TryPrepareTimeout(&ts));

            Assert.Equal(SubmitResult.SubmittedSuccessfully, r.Submit(out var submitted));
            Assert.Equal(1u, submitted);

            Assert.False(r.TryRead(out var c));

            Thread.Sleep(TimeSpan.FromSeconds(1));

            Assert.True(r.TryRead(out c));
            Assert.Equal(-ETIME, c.Result);
        }

        [Fact]
        public unsafe void TimeoutFiresAfterNCompletions()
        {
            var r = new Ring(8);

            timespec ts;
            ts.tv_sec = 10;
            ts.tv_nsec = 0;

            Assert.True(r.TryPrepareTimeout(&ts));

            Assert.Equal(SubmitResult.SubmittedSuccessfully, r.Submit(out var submitted));
            Assert.Equal(1u, submitted);

            Assert.False(r.TryRead(out var c));

            Assert.True(r.TryPrepareNop(userData: 123));
            Assert.Equal(SubmitResult.SubmittedSuccessfully, r.Submit(out submitted));
            Assert.Equal(1u, submitted);

            Assert.True(r.TryRead(out c));
            Assert.Equal(0, c.Result);
            Assert.Equal(123u, c.UserData);

            Assert.True(r.TryRead(out c));
            Assert.Equal(0, c.Result);
        }
    }
}