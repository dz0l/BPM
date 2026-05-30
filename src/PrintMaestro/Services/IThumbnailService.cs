using System.Windows.Media;

namespace PrintMaestro.Services;

public interface IThumbnailService
{
    Task<ImageSource?> GetThumbnailAsync(string filePath, CancellationToken cancellationToken = default);
}
