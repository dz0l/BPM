using PrintMaestro.Core.Configuration;

namespace PrintMaestro.Infrastructure.Configuration;

public sealed class AppPaths : IAppPaths
{
    public AppPaths()
    {
        AppDataRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PrintMaestro");

        Directory.CreateDirectory(AppDataRoot);

        SettingsFilePath = Path.Combine(AppDataRoot, "settings.json");
        HistoryDatabasePath = Path.Combine(AppDataRoot, "history.db");
        ThumbnailCachePath = Path.Combine(AppDataRoot, "thumbnails");
        LogDirectory = Path.Combine(AppDataRoot, "logs");

        Directory.CreateDirectory(ThumbnailCachePath);
        Directory.CreateDirectory(LogDirectory);
    }

    public string AppDataRoot { get; }

    public string SettingsFilePath { get; }

    public string HistoryDatabasePath { get; }

    public string ThumbnailCachePath { get; }

    public string LogDirectory { get; }
}
