using PrintMaestro.Core.Models;

namespace PrintMaestro.Core.IPC;

public sealed class WorkerMessage
{
    public required WorkerCommandType Command { get; init; }

    public Guid JobId { get; init; }

    public string FilePath { get; init; } = string.Empty;

    public PrintSettings Settings { get; init; } = new();
}
