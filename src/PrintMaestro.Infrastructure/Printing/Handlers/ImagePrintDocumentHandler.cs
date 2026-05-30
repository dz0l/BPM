using PrintMaestro.Core.Printing;
using PrintMaestro.Infrastructure.Workers;

namespace PrintMaestro.Infrastructure.Printing.Handlers;

public sealed class ImagePrintDocumentHandler : IPrintDocumentHandler
{
    private readonly IWorkerPrintService _workerPrintService;

    public ImagePrintDocumentHandler(IWorkerPrintService workerPrintService)
    {
        _workerPrintService = workerPrintService;
    }

    public DocumentKind Kind => DocumentKind.Image;

    public Task<PrintResult> PrintAsync(PrintRequest request, CancellationToken cancellationToken) =>
        _workerPrintService.PrintAsync(WorkerKind.Image, request, cancellationToken);
}
