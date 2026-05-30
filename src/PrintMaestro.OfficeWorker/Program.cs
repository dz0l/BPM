using PrintMaestro.Core.IPC;
using PrintMaestro.Infrastructure.Workers;

await WorkerPipeHost.RunServerAsync(
    WorkerPipeNames.Office,
    (request, cancellationToken) =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(OfficePrintExecutor.Execute(request, cancellationToken));
    },
    CancellationToken.None);
