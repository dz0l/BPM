namespace PrintMaestro.Core.Models;

public sealed class PrintHistoryEntry
{
    public long Id { get; init; }

    public required string FileName { get; init; }

    public required string FilePath { get; init; }

    public required string PrinterName { get; init; }

    public required string UserName { get; init; }

    public DateTimeOffset StartTime { get; init; }

    public DateTimeOffset? EndTime { get; init; }

    public bool Success { get; init; }

    public string? ErrorMessage { get; init; }

    public int Copies { get; init; }
}
