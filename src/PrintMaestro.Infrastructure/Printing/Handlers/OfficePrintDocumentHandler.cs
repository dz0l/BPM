using PrintMaestro.Core.Printing;

namespace PrintMaestro.Infrastructure.Printing.Handlers;

public sealed class OfficePrintDocumentHandler : IPrintDocumentHandler
{
    public DocumentKind Kind => DocumentKind.Office;

    public Task<PrintResult> PrintAsync(PrintRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(PrintResult.Fail("NOT_IMPLEMENTED", "Печать Office будет доступна на следующем этапе."));
    }
}
