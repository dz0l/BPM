using Docnet.Core;
using Docnet.Core.Models;
using PrintMaestro.Core.Models;
using PrintMaestro.Infrastructure.Printing;
using UglyToad.PdfPig;

var path = args.Length > 0 ? args[0] : throw new ArgumentException("PDF path required");

using var document = PdfDocument.Open(path);
Console.WriteLine($"File: {path}");
Console.WriteLine($"Pages: {document.NumberOfPages}");
Console.WriteLine();

for (var i = 1; i <= document.NumberOfPages; i++)
{
    var page = document.GetPage(i);
    var auto = PdfPageOrientationResolver.Resolve(document, i - 1, PaperOrientation.Auto);
    var effectiveW = page.Rotation.Value is 90 or 270 ? (double)page.Height : (double)page.Width;
    var effectiveH = page.Rotation.Value is 90 or 270 ? (double)page.Width : (double)page.Height;

    Console.WriteLine($"Page {i}:");
    Console.WriteLine($"  PdfPig Width={page.Width:F2} Height={page.Height:F2}");
    Console.WriteLine($"  Rotation={page.Rotation.Value} SwapsAxis={page.Rotation.SwapsAxis}");
    Console.WriteLine($"  MediaBox={page.MediaBox.Bounds}");
    Console.WriteLine($"  Effective={effectiveW:F2}x{effectiveH:F2} => Auto={auto}");
    Console.WriteLine();
}

Console.WriteLine("Docnet render (PageDimensions 1080x1920 as in PdfPrintExecutor):");
using (var docReader = DocLib.Instance.GetDocReader(path, new PageDimensions(1080, 1920)))
{
    for (var i = 0; i < docReader.GetPageCount(); i++)
    {
        using var pageReader = docReader.GetPageReader(i);
        Console.WriteLine($"  Page {i + 1}: bitmap {pageReader.GetPageWidth()}x{pageReader.GetPageHeight()}");
    }
}

Console.WriteLine();
Console.WriteLine("Docnet render (PageDimensions 1920x1080):");
using (var docReader = DocLib.Instance.GetDocReader(path, new PageDimensions(1920, 1080)))
{
    for (var i = 0; i < docReader.GetPageCount(); i++)
    {
        using var pageReader = docReader.GetPageReader(i);
        Console.WriteLine($"  Page {i + 1}: bitmap {pageReader.GetPageWidth()}x{pageReader.GetPageHeight()}");
    }
}
