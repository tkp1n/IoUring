using Xunit;

namespace IoUring.Tests
{
    public class CompletionEnumeratorTest
    {
        [Fact]
        public void SmokeTest()
        {
            using var r = new Ring(8);

            r.PrepareNop(userData: 1);
            r.PrepareNop(userData: 2);
            r.PrepareNop(userData: 3);

            Assert.Equal(SubmitResult.SubmittedSuccessfully, r.SubmitAndWait(3, out var submitted));
            Assert.Equal(3u, submitted);

            ulong i = 1;
            foreach (var completion in r.Completions)
            {
                Assert.Equal(i++, completion.userData);
            }

            Assert.Equal(4u, i);
        }
    }
}