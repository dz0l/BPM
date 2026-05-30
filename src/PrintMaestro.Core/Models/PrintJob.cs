namespace PrintMaestro.Core.Models;

public sealed class PrintJob
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public required string FilePath { get; init; }

    public string FileName => Path.GetFileName(FilePath);

    public PrintJobStatus Status { get; set; } = PrintJobStatus.Pending;

    public PrintSettings Settings { get; set; } = new();

    public int ProgressPercent { get; set; }

    public string? ErrorMessage { get; set; }

    public int RetryCount { get; set; }

    public DateTimeOffset AddedAt { get; init; } = DateTimeOffset.Now;
}
