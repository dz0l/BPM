using PrintMaestro.Core.IPC;
using PrintMaestro.Infrastructure.Workers;

await WorkerPipeHost.RunServerAsync(
    WorkerPipeNames.Pdf,
    (request, cancellationToken) =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(PdfPrintExecutor.Execute(request, cancellationToken));
    },
    CancellationToken.None);
