namespace PrintMaestro.Core.Printers;

public sealed class PrinterInfo
{
    public required string Name { get; init; }

    public bool IsDefault { get; init; }

    public bool IsNetwork { get; init; }

    public bool IsOffline { get; init; }
}

public interface IPrinterDiscoveryService
{
    Task<IReadOnlyList<PrinterInfo>> GetPrintersAsync(CancellationToken cancellationToken);
}
