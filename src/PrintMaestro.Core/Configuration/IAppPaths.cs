namespace PrintMaestro.Core.Configuration;

public interface IAppPaths
{
    string AppDataRoot { get; }

    string SettingsFilePath { get; }

    string HistoryDatabasePath { get; }

    string ThumbnailCachePath { get; }

    string LogDirectory { get; }
}
