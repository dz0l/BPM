using PrintMaestro.Core.Printing;

namespace PrintMaestro.Infrastructure.Printing.Handlers;

/// <summary>
/// Заглушка PDF-обработчика. На этапе 3 заменяется реализацией через PDFiumSharp (или аналог).
/// </summary>
public sealed class PdfPrintDocumentHandler : IPrintDocumentHandler
{
    public DocumentKind Kind => DocumentKind.Pdf;

    public Task<PrintResult> PrintAsync(PrintRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(PrintResult.Fail("NOT_IMPLEMENTED", "Печать PDF будет доступна на следующем этапе."));
    }
}
