namespace PrintMaestro.Core.IPC;

public sealed class WorkerResponse
{
    public bool Success { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public bool IsRetryable { get; init; } = true;

    public static WorkerResponse Ok() => new() { Success = true };

    public static WorkerResponse Fail(string code, string message, bool retryable = true) => new()
    {
        Success = false,
        ErrorCode = code,
        ErrorMessage = message,
        IsRetryable = retryable
    };
}
