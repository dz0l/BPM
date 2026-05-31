namespace PrintMaestro.Core.Models;

public static class SupportedFileTypes
{
    public const int MaxQueueSize = 1000;

    public static readonly IReadOnlySet<string> Extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".doc", ".docx",
        ".xls", ".xlsx",
        ".ppt", ".pptx",
        ".txt",
        ".png", ".jpg", ".jpeg", ".bmp", ".heic", ".tiff", ".webp"
    };

    public static bool IsSupported(string filePath) =>
        Extensions.Contains(Path.GetExtension(filePath));

    public static string FilterDescription =>
        "Документы|*.pdf;*.doc;*.docx;*.xls;*.xlsx;*.ppt;*.pptx;*.txt;*.png;*.jpg;*.jpeg;*.bmp;*.heic;*.tiff;*.webp|Все файлы|*.*";
}
