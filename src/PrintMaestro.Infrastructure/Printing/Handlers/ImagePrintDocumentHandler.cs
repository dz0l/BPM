using PrintMaestro.Core.Printing;

namespace PrintMaestro.Infrastructure.Printing.Handlers;

public sealed class ImagePrintDocumentHandler : IPrintDocumentHandler
{
    public DocumentKind Kind => DocumentKind.Image;

    public Task<PrintResult> PrintAsync(PrintRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(PrintResult.Fail("NOT_IMPLEMENTED", "Печать изображений будет доступна на следующем этапе."));
    }
}
