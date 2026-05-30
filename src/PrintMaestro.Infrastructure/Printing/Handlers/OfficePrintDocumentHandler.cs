using PrintMaestro.Core.Printing;
using PrintMaestro.Infrastructure.Workers;

namespace PrintMaestro.Infrastructure.Printing.Handlers;

public sealed class OfficePrintDocumentHandler : IPrintDocumentHandler
{
    private readonly IWorkerPrintService _workerPrintService;

    public OfficePrintDocumentHandler(IWorkerPrintService workerPrintService)
    {
        _workerPrintService = workerPrintService;
    }

    public DocumentKind Kind => DocumentKind.Office;

    public Task<PrintResult> PrintAsync(PrintRequest request, CancellationToken cancellationToken) =>
        _workerPrintService.PrintAsync(WorkerKind.Office, request, cancellationToken);
}
