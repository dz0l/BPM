using PrintMaestro.Core.Printing;
using PrintMaestro.Infrastructure.Workers;

namespace PrintMaestro.Infrastructure.Printing.Handlers;

public sealed class PdfPrintDocumentHandler : IPrintDocumentHandler
{
    private readonly IWorkerPrintService _workerPrintService;

    public PdfPrintDocumentHandler(IWorkerPrintService workerPrintService)
    {
        _workerPrintService = workerPrintService;
    }

    public DocumentKind Kind => DocumentKind.Pdf;

    public Task<PrintResult> PrintAsync(PrintRequest request, CancellationToken cancellationToken) =>
        _workerPrintService.PrintAsync(WorkerKind.Pdf, request, cancellationToken);
}
