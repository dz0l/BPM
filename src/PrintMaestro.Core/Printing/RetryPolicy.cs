namespace PrintMaestro.Core.Printing;

public static class RetryPolicy
{
    public const int MaxAttempts = 3;

    private static readonly TimeSpan[] BackoffDelays =
    [
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(10)
    ];

    public static TimeSpan GetBackoffDelay(int failedAttemptIndex) =>
        BackoffDelays[Math.Clamp(failedAttemptIndex, 0, BackoffDelays.Length - 1)];
}
