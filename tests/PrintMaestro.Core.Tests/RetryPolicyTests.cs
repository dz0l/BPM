using PrintMaestro.Core.Printing;

namespace PrintMaestro.Core.Tests;

public class RetryPolicyTests
{
    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 5)]
    [InlineData(2, 10)]
    [InlineData(5, 10)]
    public void GetBackoffDelay_ReturnsExpectedSeconds(int attempt, int expectedSeconds)
    {
        var delay = RetryPolicy.GetBackoffDelay(attempt);

        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), delay);
    }

    [Fact]
    public void MaxAttempts_IsThree()
    {
        Assert.Equal(3, RetryPolicy.MaxAttempts);
    }
}
