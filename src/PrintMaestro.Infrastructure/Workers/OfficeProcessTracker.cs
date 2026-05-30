using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace PrintMaestro.Infrastructure.Workers;

[SupportedOSPlatform("windows")]
internal static class OfficeProcessTracker
{
    private static readonly string SessionDirectory = Path.Combine(
        Path.GetTempPath(),
        "PrintMaestro",
        "office");

    public static void CleanupOrphanFromPreviousSession()
    {
        Directory.CreateDirectory(SessionDirectory);
        var pidFile = Path.Combine(SessionDirectory, "active.pid");

        if (!File.Exists(pidFile))
        {
            return;
        }

        var text = File.ReadAllText(pidFile).Trim();
        if (int.TryParse(text, out var pid))
        {
            TryKillProcess(pid);
        }

        TryDelete(pidFile);
    }

    public static void TrackProcess(int processId)
    {
        Directory.CreateDirectory(SessionDirectory);
        File.WriteAllText(Path.Combine(SessionDirectory, "active.pid"), processId.ToString());
    }

    public static void ClearTracking()
    {
        TryDelete(Path.Combine(SessionDirectory, "active.pid"));
    }

    public static int GetProcessIdFromWindowHandle(nint windowHandle)
    {
        if (windowHandle == 0)
        {
            return -1;
        }

        _ = GetWindowThreadProcessId(windowHandle, out var processId);
        return (int)processId;
    }

    public static void TryKillProcess(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Process already exited or access denied.
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best effort cleanup.
        }
    }

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);
}
