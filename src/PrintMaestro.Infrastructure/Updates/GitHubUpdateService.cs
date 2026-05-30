using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;
using PrintMaestro.Core.Updates;

namespace PrintMaestro.Infrastructure.Updates;

public sealed class GitHubUpdateService(HttpClient httpClient) : IUpdateService
{
    public const string DefaultRepository = "dz0l/PrintMaestro";

    public async Task<UpdateInfo?> CheckForUpdateAsync(CancellationToken cancellationToken)
    {
        var currentVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 1, 0);
        var response = await httpClient.GetFromJsonAsync<GitHubRelease>(
            $"https://api.github.com/repos/{DefaultRepository}/releases/latest",
            cancellationToken);

        if (response?.TagName is null)
        {
            return null;
        }

        var remoteVersion = ParseVersion(response.TagName);
        if (remoteVersion <= currentVersion)
        {
            return null;
        }

        var asset = response.Assets.FirstOrDefault(a =>
                         a.Name.EndsWith(".msix", StringComparison.OrdinalIgnoreCase))
                     ?? response.Assets.FirstOrDefault(a =>
                         a.Name.Contains("-Setup.exe", StringComparison.OrdinalIgnoreCase))
                     ?? response.Assets.FirstOrDefault(a =>
                         a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

        if (asset?.BrowserDownloadUrl is null)
        {
            return null;
        }

        return new UpdateInfo
        {
            Version = remoteVersion,
            DownloadUrl = asset.BrowserDownloadUrl,
            ReleaseNotes = response.Body ?? string.Empty,
            PublishedAt = response.PublishedAt
        };
    }

    public async Task DownloadAndApplyAsync(UpdateInfo update, CancellationToken cancellationToken)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "PrintMaestro", "updates", update.Version.ToString());
        Directory.CreateDirectory(tempRoot);

        var extension = Path.GetExtension(update.DownloadUrl).Split('?')[0];
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".msix";
        }

        var packagePath = Path.Combine(tempRoot, $"update{extension}");
        await using (var stream = await httpClient.GetStreamAsync(update.DownloadUrl, cancellationToken))
        await using (var file = File.Create(packagePath))
        {
            await stream.CopyToAsync(file, cancellationToken);
        }

        if (extension.Equals(".msix", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".exe", StringComparison.OrdinalIgnoreCase))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = packagePath,
                UseShellExecute = true
            });

            Environment.Exit(0);
            return;
        }

        var updaterScript = Path.Combine(tempRoot, "apply-update.cmd");
        var appDirectory = AppContext.BaseDirectory.TrimEnd('\\');
        var exeName = Path.GetFileName(Environment.ProcessPath ?? "PrintMaestro.exe");

        await File.WriteAllTextAsync(updaterScript, $"""
            @echo off
            timeout /t 2 /nobreak >nul
            tar -xf "{packagePath}" -C "{appDirectory}"
            start "" "{Path.Combine(appDirectory, exeName)}"
            del "%~f0"
            """, cancellationToken);

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = updaterScript,
            UseShellExecute = true,
            CreateNoWindow = true
        });

        Environment.Exit(0);
    }

    private static Version ParseVersion(string tagName)
    {
        var normalized = tagName.TrimStart('v', 'V');
        return Version.TryParse(normalized, out var version) ? version : new Version(0, 0, 0);
    }

    private sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; init; }

        [JsonPropertyName("body")]
        public string? Body { get; init; }

        [JsonPropertyName("published_at")]
        public DateTimeOffset PublishedAt { get; init; }

        [JsonPropertyName("assets")]
        public List<GitHubAsset> Assets { get; init; } = [];
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; init; }
    }
}
