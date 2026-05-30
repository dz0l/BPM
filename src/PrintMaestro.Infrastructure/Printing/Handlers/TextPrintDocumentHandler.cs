using PrintMaestro.Core.Printing;

namespace PrintMaestro.Infrastructure.Printing.Handlers;

public sealed class TextPrintDocumentHandler : IPrintDocumentHandler
{
    public DocumentKind Kind => DocumentKind.Text;

    public Task<PrintResult> PrintAsync(PrintRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(PrintResult.Fail("NOT_IMPLEMENTED", "Печать TXT будет доступна на следующем этапе."));
    }
}
