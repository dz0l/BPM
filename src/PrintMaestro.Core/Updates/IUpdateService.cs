namespace PrintMaestro.Core.Updates;

public sealed class UpdateInfo
{
    public required Version Version { get; init; }

    public required string DownloadUrl { get; init; }

    public required string ReleaseNotes { get; init; }

    public DateTimeOffset PublishedAt { get; init; }
}

public interface IUpdateService
{
    Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken);

    Task DownloadAndApplyAsync(UpdateInfo update, CancellationToken cancellationToken);
}
