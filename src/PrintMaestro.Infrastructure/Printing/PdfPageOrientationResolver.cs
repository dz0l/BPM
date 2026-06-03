using PrintMaestro.Core.Models;
using PrintMaestro.Core.Printing;
using UglyToad.PdfPig;

namespace PrintMaestro.Infrastructure.Printing;

/// <summary>
/// PDF metadata orientation (MediaBox/Rotate). May misdetect scanned PDFs;
/// production printing uses rendered page pixels in <see cref="Workers.PdfPrintExecutor"/>.
/// </summary>
public static class PdfPageOrientationResolver
{
    public static PaperOrientation Resolve(
        string filePath,
        int pageIndex,
        PaperOrientation userOrientation)
    {
        if (userOrientation is PaperOrientation.Portrait or PaperOrientation.Landscape)
        {
            return userOrientation;
        }

        try
        {
            using var document = PdfDocument.Open(filePath);
            return Resolve(document, pageIndex, userOrientation);
        }
        catch
        {
            return PaperOrientation.Portrait;
        }
    }

    public static PaperOrientation Resolve(
        PdfDocument document,
        int pageIndex,
        PaperOrientation userOrientation)
    {
        if (userOrientation is PaperOrientation.Portrait or PaperOrientation.Landscape)
        {
            return userOrientation;
        }

        try
        {
            var page = document.GetPage(pageIndex + 1);

            return PrintLayoutCalculator.ResolveOrientationFromPdfPage(
                userOrientation,
                (double)page.Width,
                (double)page.Height,
                page.Rotation.Value);
        }
        catch
        {
            return PaperOrientation.Portrait;
        }
    }
}
