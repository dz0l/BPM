namespace PrintMaestro.Core.Printing;

public sealed class PrintResult
{
    public bool Success { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public bool IsRetryable { get; init; } = true;

    public static PrintResult Ok() => new() { Success = true };

    public static PrintResult Fail(string code, string message, bool retryable = true) => new()
    {
        Success = false,
        ErrorCode = code,
        ErrorMessage = message,
        IsRetryable = retryable
    };
}
