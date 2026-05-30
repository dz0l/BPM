using PrintMaestro.Core.IPC;
using PrintMaestro.Core.Models;
using PrintMaestro.Core.Printing;
using PrintMaestro.Core.Threading;
using PrintMaestro.Infrastructure.Printing;
using SkiaSharp;
using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.Versioning;

namespace PrintMaestro.Infrastructure.Workers;

[SupportedOSPlatform("windows")]
public static class ImagePrintExecutor
{
    private const int MaxDimension = 3000;

    public static WorkerResponse Execute(WorkerMessage message, CancellationToken cancellationToken) =>
        StaThreadRunner.Run(() => ExecuteCore(message, cancellationToken), cancellationToken);

    private static WorkerResponse ExecuteCore(WorkerMessage message, CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(message.FilePath))
            {
                return WorkerResponse.Fail("FILE_NOT_FOUND", "File not found.", retryable: false);
            }

            using var skBitmap = SKBitmap.Decode(message.FilePath);
            if (skBitmap is null)
            {
                return WorkerResponse.Fail("INVALID_IMAGE", "Image format is not supported.", retryable: false);
            }

            if (skBitmap.Width > MaxDimension || skBitmap.Height > MaxDimension)
            {
                return WorkerResponse.Fail(
                    "IMAGE_TOO_LARGE",
                    $"Image exceeds {MaxDimension}x{MaxDimension} pixels.",
                    retryable: false);
            }

            var settings = message.Settings.Clone();
            var orientation = PrintLayoutCalculator.ResolveOrientation(
                settings.Orientation,
                skBitmap.Width,
                skBitmap.Height);

            using var bitmap = ToBitmap(skBitmap);
            using var printDocument = new PrintDocument();
            PrintDocumentHelper.Configure(printDocument, settings, orientation);

            printDocument.PrintPage += (_, e) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var graphics = e.Graphics ?? throw new InvalidOperationException("Graphics context is unavailable.");
                PrintDocumentHelper.DrawImageFit(graphics, bitmap, e.MarginBounds, settings.FitToPage);
                e.HasMorePages = false;
            };

            printDocument.Print();

            if (settings.InsertBlankPageAfter)
            {
                PrintDocumentHelper.PrintBlankPage(settings, orientation, cancellationToken);
            }

            return WorkerResponse.Ok();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return WorkerResponse.Fail("IMAGE_PRINT_FAILED", ex.Message);
        }
    }

    private static Bitmap ToBitmap(SKBitmap skBitmap)
    {
        using var image = SKImage.FromBitmap(skBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new MemoryStream(data.ToArray());
        return new Bitmap(stream);
    }
}
