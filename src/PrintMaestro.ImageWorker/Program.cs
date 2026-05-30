using PrintMaestro.Core.IPC;
using PrintMaestro.Infrastructure.Workers;

await WorkerPipeHost.RunServerAsync(
    WorkerPipeNames.Image,
    (request, cancellationToken) =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ImagePrintExecutor.Execute(request, cancellationToken));
    },
    CancellationToken.None);
