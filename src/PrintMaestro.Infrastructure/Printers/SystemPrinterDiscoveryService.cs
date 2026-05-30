using System.Drawing.Printing;
using System.Runtime.Versioning;
using PrintMaestro.Core.Printers;

namespace PrintMaestro.Infrastructure.Printers;

[SupportedOSPlatform("windows")]
public sealed class SystemPrinterDiscoveryService : IPrinterDiscoveryService
{
    public Task<IReadOnlyList<PrinterInfo>> GetPrintersAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var defaultPrinter = GetDefaultPrinterName();
        var printers = PrinterSettings.InstalledPrinters
            .Cast<string>()
            .Select(name => new PrinterInfo
            {
                Name = name,
                IsDefault = string.Equals(name, defaultPrinter, StringComparison.OrdinalIgnoreCase),
                IsNetwork = name.StartsWith("\\\\", StringComparison.Ordinal),
                IsOffline = false
            })
            .ToList();

        return Task.FromResult<IReadOnlyList<PrinterInfo>>(printers);
    }

    private static string? GetDefaultPrinterName()
    {
        try
        {
            var settings = new PrinterSettings();
            return settings.PrinterName;
        }
        catch
        {
            return null;
        }
    }
}
