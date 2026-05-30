using PrintMaestro.Core.Models;
using PrintMaestro.Core.Printing;
using PrintMaestro.Core.Threading;
using PrintMaestro.Infrastructure.Printing;
using System.Drawing;
using System.Drawing.Printing;
using System.Runtime.Versioning;

namespace PrintMaestro.Infrastructure.Printing.Handlers;

[SupportedOSPlatform("windows")]
public sealed class TextPrintDocumentHandler : IPrintDocumentHandler
{
    public DocumentKind Kind => DocumentKind.Text;

    public Task<PrintResult> PrintAsync(PrintRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(
            () => StaThreadRunner.Run(() => PrintCore(request, cancellationToken), cancellationToken),
            cancellationToken);
    }

    private static PrintResult PrintCore(PrintRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (!File.Exists(request.FilePath))
            {
                return PrintResult.Fail("FILE_NOT_FOUND", "File not found.", retryable: false);
            }

            var settings = request.Settings;
            var orientation = PrintLayoutCalculator.ResolveOrientation(
                settings.Orientation,
                contentWidth: 0,
                contentHeight: 0);

            var lines = File.ReadAllLines(request.FilePath);
            using var printDocument = new PrintDocument();
            PrintDocumentHelper.Configure(printDocument, settings, orientation);

            var state = new TextPrintState(lines);
            printDocument.PrintPage += (_, e) => PrintPage(e, state, cancellationToken);

            printDocument.Print();

            if (settings.InsertBlankPageAfter)
            {
                PrintDocumentHelper.PrintBlankPage(settings, orientation, cancellationToken);
            }

            return PrintResult.Ok();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return PrintResult.Fail("PRINT_FAILED", ex.Message);
        }
    }

    private static void PrintPage(PrintPageEventArgs e, TextPrintState state, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var graphics = e.Graphics ?? throw new InvalidOperationException("Graphics context is unavailable.");
        var bounds = e.MarginBounds;
        using var font = new Font(FontFamily.GenericMonospace, 10f);
        var lineHeight = font.GetHeight(graphics);
        var y = (float)bounds.Top;

        while (state.LineIndex < state.Lines.Length)
        {
            if (y + lineHeight > bounds.Bottom)
            {
                e.HasMorePages = true;
                return;
            }

            graphics.DrawString(state.Lines[state.LineIndex], font, Brushes.Black, bounds.Left, y);
            y += lineHeight;
            state.LineIndex++;
        }

        e.HasMorePages = false;
    }

    private sealed class TextPrintState(string[] lines)
    {
        public string[] Lines { get; } = lines;

        public int LineIndex { get; set; }
    }
}
