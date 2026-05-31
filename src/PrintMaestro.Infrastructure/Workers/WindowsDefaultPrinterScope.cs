using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace PrintMaestro.Infrastructure.Workers;

[SupportedOSPlatform("windows")]
internal sealed class WindowsDefaultPrinterScope : IDisposable
{
    private readonly string? _previousPrinter;
    private bool _disposed;

    private WindowsDefaultPrinterScope(string? previousPrinter)
    {
        _previousPrinter = previousPrinter;
    }

    public static WindowsDefaultPrinterScope? TryCreate(string? desiredPrinterName)
    {
        if (string.IsNullOrWhiteSpace(desiredPrinterName))
        {
            return null;
        }

        var previous = TryGetDefaultPrinterName();
        SetDefaultPrinter(desiredPrinterName);

        if (string.Equals(previous, desiredPrinterName, StringComparison.OrdinalIgnoreCase))
        {
            return new WindowsDefaultPrinterScope(null);
        }

        return new WindowsDefaultPrinterScope(previous);
    }

    public void Dispose()
    {
        if (_disposed || string.IsNullOrWhiteSpace(_previousPrinter))
        {
            _disposed = true;
            return;
        }

        try
        {
            SetDefaultPrinter(_previousPrinter);
        }
        catch
        {
            // Best effort restore after print.
        }

        _disposed = true;
    }

    private static string? TryGetDefaultPrinterName()
    {
        const string keyPath = @"Software\Microsoft\Windows NT\CurrentVersion\Windows";
        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(keyPath);
        var device = key?.GetValue("Device") as string;
        if (string.IsNullOrWhiteSpace(device))
        {
            return null;
        }

        var commaIndex = device.IndexOf(',');
        return commaIndex >= 0 ? device[..commaIndex] : device;
    }

    private static void SetDefaultPrinter(string printerName)
    {
        var networkType = Type.GetTypeFromProgID("WScript.Network")
            ?? throw new InvalidOperationException("WScript.Network is not available.");

        dynamic? network = null;

        try
        {
            network = Activator.CreateInstance(networkType)!;
            network.SetDefaultPrinter(printerName);
        }
        finally
        {
            if (network is not null)
            {
                Marshal.ReleaseComObject(network);
            }
        }
    }
}
