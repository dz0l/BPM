using PrintMaestro.Core.Models;
using PrintMaestro.Core.Printing;

namespace PrintMaestro.Core.Configuration;

public sealed class AppSettings
{
    public string DefaultPrinterName { get; set; } = string.Empty;

    public PrintSettings DefaultPrintSettings { get; set; } = new();

    public bool CheckUpdatesOnStartup { get; set; } = true;

    public int MaxConcurrentJobs { get; set; } = 1;

    public PrintErrorPolicy OnPrintError { get; set; } = PrintErrorPolicy.Retry;

    public int PrintJobTimeoutSeconds { get; set; } = (int)PrintDispatchDefaults.JobWatchdogTimeout.TotalSeconds;

    public string UpdateChannel { get; set; } = "stable";

    /// <summary>
    /// UI culture code (en, ru). Empty on first launch — auto-detected from Windows.
    /// </summary>
    public string? Locale { get; set; }

    public List<PrintProfile> PrintProfiles { get; set; } = [];
}
