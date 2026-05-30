using PrintMaestro.Core.Configuration;

namespace PrintMaestro.Core.Configuration;

public interface ISettingsStore
{
    AppSettings Load();

    Task<AppSettings> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken);
}
