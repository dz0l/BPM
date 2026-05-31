namespace PrintMaestro.Core.Printing;

public static class DocumentKindResolver
{
    public static DocumentKind Resolve(string filePath)
    {
        var extension = Path.GetExtension(filePath);

        return extension.ToLowerInvariant() switch
        {
            ".pdf" => DocumentKind.Pdf,
            ".doc" or ".docx" or ".xls" or ".xlsx" or ".ppt" or ".pptx" => DocumentKind.Office,
            ".png" or ".jpg" or ".jpeg" or ".bmp" or ".heic" or ".tiff" or ".webp" => DocumentKind.Image,
            ".txt" => DocumentKind.Text,
            _ => DocumentKind.Unknown
        };
    }
}
