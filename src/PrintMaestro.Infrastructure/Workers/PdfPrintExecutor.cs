using Docnet.Core;
using Docnet.Core.Models;
using Docnet.Core.Readers;
using PrintMaestro.Core.IPC;
using PrintMaestro.Core.Models;
using PrintMaestro.Core.Threading;
using PrintMaestro.Infrastructure.Printing;
using UglyToad.PdfPig;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace PrintMaestro.Infrastructure.Workers;

[SupportedOSPlatform("windows")]
public static class PdfPrintExecutor
{
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

            var fileInfo = new FileInfo(message.FilePath);
            if (fileInfo.Length > 1024L * 1024 * 1024)
            {
                return WorkerResponse.Fail("FILE_TOO_LARGE", "PDF exceeds the 1 GB limit.", retryable: false);
            }

            using var docReader = DocLib.Instance.GetDocReader(message.FilePath, new PageDimensions(1080, 1920));
            var pageCount = docReader.GetPageCount();

            if (pageCount == 0)
            {
                return WorkerResponse.Fail("EMPTY_PDF", "PDF has no pages.", retryable: false);
            }

            var settings = message.Settings.Clone();

            using var pdfDocument = settings.Orientation == PaperOrientation.Auto
                ? PdfDocument.Open(message.FilePath)
                : null;

            PaperOrientation? initialOrientation = pdfDocument is not null
                ? PdfPageOrientationResolver.Resolve(pdfDocument, 0, PaperOrientation.Auto)
                : settings.Orientation is PaperOrientation.Portrait or PaperOrientation.Landscape
                    ? settings.Orientation
                    : null;

            using var printDocument = new PrintDocument();
            PrintDocumentHelper.Configure(printDocument, settings, initialOrientation);

            var state = new PdfPrintState(pdfDocument, docReader, pageCount, settings);
            printDocument.QueryPageSettings += (_, e) => QueryPageSettings(e, state);
            printDocument.PrintPage += (_, e) => PrintPage(e, state, cancellationToken);
            printDocument.Print();

            if (settings.InsertBlankPageAfter)
            {
                PrintDocumentHelper.PrintBlankPage(settings, state.LastOrientation, cancellationToken);
            }

            return WorkerResponse.Ok();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return WorkerResponse.Fail("PDF_PRINT_FAILED", ex.Message);
        }
    }

    private static void QueryPageSettings(QueryPageSettingsEventArgs e, PdfPrintState state)
    {
        if (state.PageIndex >= state.PageCount)
        {
            return;
        }

        var orientation = state.PdfDocument is not null
            ? PdfPageOrientationResolver.Resolve(state.PdfDocument, state.PageIndex, state.Settings.Orientation)
            : state.Settings.Orientation;

        PrintDocumentHelper.ApplyPageOrientation(e.PageSettings, state.Settings, orientation);
        state.LastOrientation = orientation;
    }

    private static void PrintPage(PrintPageEventArgs e, PdfPrintState state, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (state.PageIndex >= state.PageCount)
        {
            e.HasMorePages = false;
            return;
        }

        using var pageReader = state.DocReader.GetPageReader(state.PageIndex);
        using var bitmap = CreateBitmap(pageReader);
        var graphics = e.Graphics ?? throw new InvalidOperationException("Graphics context is unavailable.");
        PrintDocumentHelper.DrawImageFit(graphics, bitmap, e.MarginBounds, state.Settings.FitToPage);

        state.PageIndex++;
        e.HasMorePages = state.PageIndex < state.PageCount;
    }

    private static Bitmap CreateBitmap(IPageReader pageReader)
    {
        var width = pageReader.GetPageWidth();
        var height = pageReader.GetPageHeight();
        var bytes = pageReader.GetImage();

        var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        var rect = new Rectangle(0, 0, width, height);
        var bitmapData = bitmap.LockBits(rect, ImageLockMode.WriteOnly, bitmap.PixelFormat);

        try
        {
            Marshal.Copy(bytes, 0, bitmapData.Scan0, bytes.Length);
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        return bitmap;
    }

    private sealed class PdfPrintState(
        PdfDocument? pdfDocument,
        IDocReader docReader,
        int pageCount,
        PrintSettings settings)
    {
        public PdfDocument? PdfDocument { get; } = pdfDocument;

        public IDocReader DocReader { get; } = docReader;

        public int PageCount { get; } = pageCount;

        public PrintSettings Settings { get; } = settings;

        public int PageIndex { get; set; }

        public PaperOrientation LastOrientation { get; set; } = PaperOrientation.Portrait;
    }
}
