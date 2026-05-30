using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using PrintMaestro.Core.Configuration;
using PrintMaestro.Core.Localization;

namespace PrintMaestro.Services;

public sealed class JsonLocalizationService : ILocalizationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private readonly ISettingsStore _settingsStore;
    private readonly Dictionary<string, string> _strings = new(StringComparer.Ordinal);
    private ResourceDictionary? _targetDictionary;

    public JsonLocalizationService(ISettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
        SupportedCultures = DiscoverSupportedCultures();
    }

    public string CurrentCulture { get; private set; } = LocaleDetector.DefaultCulture;

    public IReadOnlyList<string> SupportedCultures { get; }

    public event EventHandler? CultureChanged;

    public void SetTargetDictionary(ResourceDictionary dictionary) => _targetDictionary = dictionary;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken);
        var culture = settings.Locale;

        if (string.IsNullOrWhiteSpace(culture) || !SupportedCultures.Contains(culture))
        {
            culture = LocaleDetector.DetectSupportedCulture(SupportedCultures);
            settings.Locale = culture;
            await _settingsStore.SaveAsync(settings, cancellationToken);
        }

        await ApplyCultureAsync(culture, persist: false, cancellationToken);
    }

    public async Task ApplyCultureAsync(string culture, CancellationToken cancellationToken = default)
    {
        await ApplyCultureAsync(culture, persist: true, cancellationToken);
    }

    public string GetString(string key, params object[] args)
    {
        if (!_strings.TryGetValue(key, out var value))
        {
            value = key;
        }

        return args.Length > 0
            ? string.Format(CultureInfo.CurrentCulture, value, args)
            : value;
    }

    private async Task ApplyCultureAsync(string culture, bool persist, CancellationToken cancellationToken)
    {
        if (!SupportedCultures.Contains(culture))
        {
            culture = LocaleDetector.DefaultCulture;
        }

        _strings.Clear();
        foreach (var pair in LoadLocaleFile(culture))
        {
            _strings[pair.Key] = pair.Value;
        }

        if (culture != LocaleDetector.DefaultCulture)
        {
            foreach (var pair in LoadLocaleFile(LocaleDetector.DefaultCulture))
            {
                _strings.TryAdd(pair.Key, pair.Value);
            }
        }

        CurrentCulture = culture;
        ApplyToResourceDictionary();

        if (persist)
        {
            var settings = await _settingsStore.LoadAsync(cancellationToken);
            settings.Locale = culture;
            await _settingsStore.SaveAsync(settings, cancellationToken);
        }

        CultureChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyToResourceDictionary()
    {
        if (_targetDictionary is null)
        {
            return;
        }

        _targetDictionary.Clear();

        foreach (var pair in _strings)
        {
            _targetDictionary[pair.Key] = pair.Value;
        }
    }

    private Dictionary<string, string> LoadLocaleFile(string culture)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Localization", $"locale.{culture}.json");
        if (!File.Exists(path))
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions)
               ?? new Dictionary<string, string>(StringComparer.Ordinal);
    }

    private static IReadOnlyList<string> DiscoverSupportedCultures()
    {
        var localizationRoot = Path.Combine(AppContext.BaseDirectory, "Assets", "Localization");
        if (!Directory.Exists(localizationRoot))
        {
            return [LocaleDetector.DefaultCulture];
        }

        return Directory
            .EnumerateFiles(localizationRoot, "locale.*.json")
            .Select(path => Path.GetFileNameWithoutExtension(path)!.Replace("locale.", string.Empty, StringComparison.Ordinal))
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
