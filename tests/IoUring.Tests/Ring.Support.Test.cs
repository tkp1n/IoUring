using System;
using Xunit;

namespace IoUring.Tests
{
    public class RingSupportTest 
    {
        [Fact]
        public void SmokeTest() 
        {
            Assert.True(Ring.IsSupported);

            using var r = new Ring(8);

            foreach (var i in Enum.GetValues(typeof(RingOperation)))
            {
                Assert.True(r.Supports((RingOperation)i));
            }
        }
    }
}