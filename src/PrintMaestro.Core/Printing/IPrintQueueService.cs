using PrintMaestro.Core.Models;

namespace PrintMaestro.Core.Printing;

public interface IPrintQueueService
{
    IReadOnlyList<PrintJob> Jobs { get; }

    event EventHandler? QueueChanged;

    bool CanAdd(int count = 1);

    PrintJob Add(string filePath, PrintSettings? settings = null);

    IReadOnlyList<PrintJob> AddRange(IEnumerable<string> filePaths, PrintSettings? settings = null);

    void Remove(Guid jobId);

    void Reorder(int oldIndex, int newIndex);

    void ClearCompleted();

    void UpdateStatus(Guid jobId, PrintJobStatus status, int progressPercent = 0, string? errorMessage = null);
}
