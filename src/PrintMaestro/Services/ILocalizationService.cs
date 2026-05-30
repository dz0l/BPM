namespace PrintMaestro.Services;

public interface ILocalizationService
{
    string CurrentCulture { get; }

    IReadOnlyList<string> SupportedCultures { get; }

    event EventHandler? CultureChanged;

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task ApplyCultureAsync(string culture, CancellationToken cancellationToken = default);

    string GetString(string key, params object[] args);
}
