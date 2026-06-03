using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using PrintMaestro.Core.Models;

namespace PrintMaestro.Infrastructure.Printing;

public static class PrintDocumentHelper
{
    public static void Configure(
        PrintDocument printDocument,
        PrintSettings settings,
        PaperOrientation? orientationOverride = null)
    {
        if (!string.IsNullOrWhiteSpace(settings.PrinterName))
        {
            printDocument.PrinterSettings.PrinterName = settings.PrinterName;
        }

        if (!printDocument.PrinterSettings.IsValid)
        {
            throw new InvalidOperationException($"Printer is not available: {settings.PrinterName}");
        }

        printDocument.PrinterSettings.Copies = (short)Math.Clamp(settings.Copies, 1, short.MaxValue);
        printDocument.PrinterSettings.Duplex = settings.Duplex switch
        {
            DuplexMode.DuplexLongEdge => Duplex.Vertical,
            DuplexMode.DuplexShortEdge => Duplex.Horizontal,
            _ => Duplex.Simplex
        };

        ApplyPaperSettings(printDocument.DefaultPageSettings, settings, orientationOverride);
    }

    public static void ApplyPaperSettings(
        PageSettings pageSettings,
        PrintSettings settings,
        PaperOrientation? orientationOverride = null)
    {
        var orientation = orientationOverride ?? settings.Orientation;
        if (orientation == PaperOrientation.Auto)
        {
            orientation = PaperOrientation.Portrait;
        }

        pageSettings.Landscape = orientation == PaperOrientation.Landscape;

        var paperKind = settings.PaperFormat == PaperFormat.A3 ? PaperKind.A3 : PaperKind.A4;
        var paperSize = pageSettings.PrinterSettings.PaperSizes
            .Cast<PaperSize>()
            .FirstOrDefault(size => size.Kind == paperKind);

        if (paperSize is not null)
        {
            pageSettings.PaperSize = paperSize;
        }
    }

    public static void ApplyPageOrientation(PageSettings pageSettings, PrintSettings settings, PaperOrientation orientation)
    {
        ApplyPaperSettings(pageSettings, settings, orientation);
    }

    public static void PrintBlankPage(
        PrintSettings settings,
        PaperOrientation orientation,
        CancellationToken cancellationToken)
    {
        using var printDocument = new PrintDocument();
        Configure(printDocument, settings, orientation);

        printDocument.PrintPage += (_, e) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            e.HasMorePages = false;
        };

        printDocument.Print();
    }

    public static RectangleF CalculateFitBounds(SizeF imageSize, RectangleF pageBounds)
    {
        if (imageSize.Width <= 0 || imageSize.Height <= 0)
        {
            return pageBounds;
        }

        var scale = Math.Min(pageBounds.Width / imageSize.Width, pageBounds.Height / imageSize.Height);
        var width = imageSize.Width * scale;
        var height = imageSize.Height * scale;
        var x = pageBounds.Left + (pageBounds.Width - width) / 2f;
        var y = pageBounds.Top + (pageBounds.Height - height) / 2f;
        return new RectangleF(x, y, width, height);
    }

    public static void DrawImageFit(Graphics graphics, Image image, RectangleF pageBounds, bool fitToPage)
    {
        var imageSize = new SizeF(image.Width, image.Height);
        var target = fitToPage
            ? CalculateFitBounds(imageSize, pageBounds)
            : new RectangleF(pageBounds.Left, pageBounds.Top, imageSize.Width, imageSize.Height);

        var needsResample = Math.Abs(target.Width - imageSize.Width) > 1f
            || Math.Abs(target.Height - imageSize.Height) > 1f;

        if (!needsResample)
        {
            graphics.DrawImage(image, target);
            return;
        }

        var state = graphics.Save();
        try
        {
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.DrawImage(image, target);
        }
        finally
        {
            graphics.Restore(state);
        }
    }
}

