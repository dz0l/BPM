using System.Text.Json;
using System.Text.Json.Serialization;
using PrintMaestro.Core.Configuration;

namespace PrintMaestro.Infrastructure.Configuration;

public sealed class JsonSettingsStore(IAppPaths appPaths) : ISettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: true) }
    };

    public AppSettings Load()
    {
        if (!File.Exists(appPaths.SettingsFilePath))
        {
            return AppSettingsNormalizer.Normalize(new AppSettings());
        }

        var json = File.ReadAllText(appPaths.SettingsFilePath);
        var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
        return AppSettingsNormalizer.Normalize(settings ?? new AppSettings());
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(appPaths.SettingsFilePath))
        {
            return new AppSettings();
        }

        await using var stream = File.OpenRead(appPaths.SettingsFilePath);
        var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, cancellationToken);
        return AppSettingsNormalizer.Normalize(settings ?? new AppSettings());
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        await using var stream = File.Create(appPaths.SettingsFilePath);
        await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken);
    }
}
