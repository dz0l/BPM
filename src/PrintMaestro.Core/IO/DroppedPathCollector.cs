namespace PrintMaestro.Core.IO;

public sealed class DroppedPathCollectionResult
{
    public required IReadOnlyList<string> SupportedFiles { get; init; }

    public int SkippedCount { get; init; }
}

public static class DroppedPathCollector
{
    public static DroppedPathCollectionResult Collect(IEnumerable<string> paths)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var skipped = 0;

        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            if (Directory.Exists(path))
            {
                foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
                {
                    if (Models.SupportedFileTypes.IsSupported(file))
                    {
                        result.Add(file);
                    }
                    else
                    {
                        skipped++;
                    }
                }
            }
            else if (File.Exists(path))
            {
                if (Models.SupportedFileTypes.IsSupported(path))
                {
                    result.Add(path);
                }
                else
                {
                    skipped++;
                }
            }
        }

        return new DroppedPathCollectionResult
        {
            SupportedFiles = result.ToList(),
            SkippedCount = skipped
        };
    }

    public static IReadOnlyList<string> CollectSupportedFiles(IEnumerable<string> paths) =>
        Collect(paths).SupportedFiles;
}
