using System.Reflection;

namespace PrintMaestro.Core;

public static class AppVersionInfo
{
    public static Version Current
    {
        get
        {
            var version = Assembly.GetEntryAssembly()?.GetName().Version
                          ?? Assembly.GetExecutingAssembly().GetName().Version;
            return version ?? new Version(0, 0, 0);
        }
    }

    public static string CurrentDisplay => Current.ToString(3);
}
