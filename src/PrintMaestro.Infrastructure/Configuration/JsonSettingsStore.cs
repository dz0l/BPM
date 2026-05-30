using System.Text.Json;
using PrintMaestro.Core.Configuration;

namespace PrintMaestro.Infrastructure.Configuration;

public sealed class JsonSettingsStore(IAppPaths appPaths) : ISettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(appPaths.SettingsFilePath))
        {
            return new AppSettings();
        }

        await using var stream = File.OpenRead(appPaths.SettingsFilePath);
        var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, cancellationToken);
        return settings ?? new AppSettings();
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        await using var stream = File.Create(appPaths.SettingsFilePath);
        await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken);
    }
}
