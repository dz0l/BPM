namespace PrintMaestro.ViewModels;

public sealed class PrintHistoryEntryViewModel
{
    public required string FileName { get; init; }

    public required string PrinterName { get; init; }

    public required string StartedAtDisplay { get; init; }

    public required string ResultDisplay { get; init; }

    public string? ErrorMessage { get; init; }

    public int Copies { get; init; }
}
