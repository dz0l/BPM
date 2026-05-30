using System.IO;
using System.Windows.Media;
using PrintMaestro.Core.Configuration;

namespace PrintMaestro.Services;

public sealed class ThumbnailService : IThumbnailService
{
    private const int CacheCapacity = 128;

    private readonly IAppPaths _appPaths;
    private readonly ShellThumbnailProvider _shellProvider = new();
    private readonly LruCache<string, ImageSource> _memoryCache = new(CacheCapacity);

    public ThumbnailService(IAppPaths appPaths)
    {
        _appPaths = appPaths;
    }

    public Task<ImageSource?> GetThumbnailAsync(string filePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return Task.FromResult<ImageSource?>(null);
        }

        var cacheKey = BuildCacheKey(filePath);
        if (_memoryCache.TryGet(cacheKey, out var cached))
        {
            return Task.FromResult<ImageSource?>(cached);
        }

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var diskPath = GetDiskCachePath(cacheKey);
            if (File.Exists(diskPath))
            {
                var fromDisk = LoadFromDisk(diskPath);
                if (fromDisk is not null)
                {
                    _memoryCache.Set(cacheKey, fromDisk);
                    return fromDisk;
                }
            }

            var thumbnail = _shellProvider.GetThumbnail(filePath);
            if (thumbnail is null)
            {
                return null;
            }

            SaveToDisk(diskPath, thumbnail);
            _memoryCache.Set(cacheKey, thumbnail);
            return thumbnail;
        }, cancellationToken);
    }

    private static string BuildCacheKey(string filePath)
    {
        var info = new FileInfo(filePath);
        return $"{info.FullName.ToLowerInvariant()}|{info.LastWriteTimeUtc.Ticks}";
    }

    private string GetDiskCachePath(string cacheKey)
    {
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(cacheKey)));

        return Path.Combine(_appPaths.ThumbnailCachePath, $"{hash}.png");
    }

    private static ImageSource? LoadFromDisk(string path)
    {
        try
        {
            var image = new System.Windows.Media.Imaging.BitmapImage();
            image.BeginInit();
            image.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(path);
            image.EndInit();
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }

    private static void SaveToDisk(string path, ImageSource source)
    {
        try
        {
            if (source is not System.Windows.Media.Imaging.BitmapSource bitmapSource)
            {
                return;
            }

            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmapSource));
            using var stream = File.Create(path);
            encoder.Save(stream);
        }
        catch
        {
            // Thumbnail disk cache is best effort.
        }
    }
}
