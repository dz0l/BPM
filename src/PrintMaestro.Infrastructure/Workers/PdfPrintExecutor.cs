using Docnet.Core;
using Docnet.Core.Models;
using Docnet.Core.Readers;
using PrintMaestro.Core.Configuration;
using PrintMaestro.Core.IPC;
using PrintMaestro.Core.Models;
using PrintMaestro.Core.Printing;
using PrintMaestro.Core.Threading;
using PrintMaestro.Infrastructure.Printing;
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

            var settings = message.Settings.Clone();
            var renderDpi = PdfRenderDpiOptions.Clamp(message.PdfRenderDpi);
            using var docReader = DocLib.Instance.GetDocReader(
                message.FilePath,
                new PageDimensions(renderDpi / 72.0));

            var pageCount = docReader.GetPageCount();
            if (pageCount == 0)
            {
                return WorkerResponse.Fail("EMPTY_PDF", "PDF has no pages.", retryable: false);
            }

            using var firstPageReader = docReader.GetPageReader(0);
            var initialOrientation = ResolvePageOrientation(
                settings,
                firstPageReader.GetPageWidth(),
                firstPageReader.GetPageHeight());

            using var printDocument = new PrintDocument();
            PrintDocumentHelper.Configure(printDocument, settings, initialOrientation);

            var state = new PdfPrintState(docReader, pageCount, settings);
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

        using var pageReader = state.DocReader.GetPageReader(state.PageIndex);
        var orientation = ResolvePageOrientation(
            state.Settings,
            pageReader.GetPageWidth(),
            pageReader.GetPageHeight());

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
        var printBounds = GetPrintBounds(e, state.Settings.FitToPage);
        PrintDocumentHelper.DrawImageFit(graphics, bitmap, printBounds, state.Settings.FitToPage);

        state.PageIndex++;
        e.HasMorePages = state.PageIndex < state.PageCount;
    }

    private static PaperOrientation ResolvePageOrientation(
        PrintSettings settings,
        int contentWidth,
        int contentHeight) =>
        PrintLayoutCalculator.ResolveOrientation(settings.Orientation, contentWidth, contentHeight);

    private static RectangleF GetPrintBounds(PrintPageEventArgs e, bool fitToPage)
    {
        if (fitToPage)
        {
            return new RectangleF(e.PageBounds.Left, e.PageBounds.Top, e.PageBounds.Width, e.PageBounds.Height);
        }

        return e.MarginBounds;
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

    private sealed class PdfPrintState(IDocReader docReader, int pageCount, PrintSettings settings)
    {
        public IDocReader DocReader { get; } = docReader;

        public int PageCount { get; } = pageCount;

        public PrintSettings Settings { get; } = settings;

        public int PageIndex { get; set; }

        public PaperOrientation LastOrientation { get; set; } = PaperOrientation.Portrait;
    }
}
