using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace PrintMaestro.Infrastructure.Workers;

[SupportedOSPlatform("windows")]
internal static class OfficePrinterResolver
{
    private static readonly object CacheLock = new();
    private static string? _cachedDesiredPrinterName;
    private static string? _cachedResolvedPrinterName;

    public static string? TryResolveActivePrinterName(string desiredPrinterName)
    {
        if (string.IsNullOrWhiteSpace(desiredPrinterName))
        {
            return null;
        }

        lock (CacheLock)
        {
            if (_cachedDesiredPrinterName == desiredPrinterName
                && !string.IsNullOrWhiteSpace(_cachedResolvedPrinterName))
            {
                return _cachedResolvedPrinterName;
            }
        }

        var resolved = ProbeWithWord(desiredPrinterName)
            ?? ProbeWithExcel(desiredPrinterName);

        lock (CacheLock)
        {
            _cachedDesiredPrinterName = desiredPrinterName;
            _cachedResolvedPrinterName = resolved;
        }

        return resolved;
    }

    public static string? TryResolveActivePrinter(dynamic app, string desiredPrinterName)
    {
        if (string.IsNullOrWhiteSpace(desiredPrinterName))
        {
            return null;
        }

        var saved = TryGetActivePrinter(app);

        try
        {
            if (saved is not null && MatchesPrinter(saved, desiredPrinterName))
            {
                return saved;
            }

            foreach (string installed in PrinterSettings.InstalledPrinters)
            {
                if (!MatchesPrinter(installed, desiredPrinterName))
                {
                    continue;
                }

                for (var port = 0; port <= 15; port++)
                {
                    var candidate = $"{installed} on Ne{port:D2}:";
                    if (TrySetActivePrinter(app, candidate, out string resolved)
                        && MatchesPrinter(resolved, desiredPrinterName))
                    {
                        RestoreActivePrinter(app, saved);
                        return resolved;
                    }
                }
            }

            if (TrySetActivePrinter(app, desiredPrinterName, out string direct))
            {
                RestoreActivePrinter(app, saved);
                return direct;
            }
        }
        finally
        {
            RestoreActivePrinter(app, saved);
        }

        return null;
    }

    public static bool TrySetActivePrinterOption(dynamic printOptions, string printerName)
    {
        try
        {
            printOptions.ActivePrinter = printerName;
            return true;
        }
        catch (Exception ex) when (IsComRelated(ex))
        {
            return false;
        }
    }

    public static bool TrySetActivePrinter(dynamic app, string printerName, out string resolved)
    {
        resolved = string.Empty;

        try
        {
            app.ActivePrinter = printerName;
            resolved = (string)app.ActivePrinter;
            return true;
        }
        catch (Exception ex) when (IsComRelated(ex))
        {
            return false;
        }
    }

    private static string? ProbeWithWord(string desiredPrinterName)
    {
        var wordType = Type.GetTypeFromProgID("Word.Application");
        if (wordType is null)
        {
            return null;
        }

        dynamic? app = null;

        try
        {
            app = Activator.CreateInstance(wordType)!;
            app.Visible = false;
            app.DisplayAlerts = 0;
            return TryResolveActivePrinter(app, desiredPrinterName);
        }
        catch (Exception ex) when (IsComRelated(ex))
        {
            return null;
        }
        finally
        {
            if (app is not null)
            {
                try
                {
                    app.Quit(SaveChanges: false);
                }
                catch
                {
                    // Ignore quit errors.
                }

                try
                {
                    Marshal.ReleaseComObject(app);
                }
                catch
                {
                    // Ignore release errors.
                }
            }
        }
    }

    private static string? ProbeWithExcel(string desiredPrinterName)
    {
        var excelType = Type.GetTypeFromProgID("Excel.Application");
        if (excelType is null)
        {
            return null;
        }

        dynamic? app = null;

        try
        {
            app = Activator.CreateInstance(excelType)!;
            app.Visible = false;
            app.DisplayAlerts = false;
            return TryResolveActivePrinter(app, desiredPrinterName);
        }
        catch (Exception ex) when (IsComRelated(ex))
        {
            return null;
        }
        finally
        {
            if (app is not null)
            {
                try
                {
                    app.Quit();
                }
                catch
                {
                    // Ignore quit errors.
                }

                try
                {
                    Marshal.ReleaseComObject(app);
                }
                catch
                {
                    // Ignore release errors.
                }
            }
        }
    }

    private static string? TryGetActivePrinter(dynamic app)
    {
        try
        {
            return (string)app.ActivePrinter;
        }
        catch (Exception ex) when (IsComRelated(ex))
        {
            return null;
        }
    }

    private static void RestoreActivePrinter(dynamic app, string? saved)
    {
        if (string.IsNullOrWhiteSpace(saved))
        {
            return;
        }

        TrySetActivePrinter(app, saved, out string _);
    }

    private static bool IsComRelated(Exception exception) =>
        exception is COMException
        || exception.InnerException is COMException
        || exception.Message.Contains("ActivePrinter", StringComparison.OrdinalIgnoreCase);

    private static bool MatchesPrinter(string candidate, string desiredPrinterName) =>
        candidate.StartsWith(desiredPrinterName, StringComparison.OrdinalIgnoreCase)
        || candidate.Contains(desiredPrinterName, StringComparison.OrdinalIgnoreCase);
}
