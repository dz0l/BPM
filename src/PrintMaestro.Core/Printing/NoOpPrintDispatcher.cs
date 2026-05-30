using PrintMaestro.Core.Models;

namespace PrintMaestro.Core.Printing;

/// <summary>
/// Заглушка диспетчера для MVP. Реальная sequential dispatch-логика добавляется на этапе печати.
/// </summary>
public sealed class NoOpPrintDispatcher : IPrintDispatcher
{
    public bool IsRunning { get; private set; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        IsRunning = true;
        return Task.CompletedTask;
    }

    public Task PauseAsync(CancellationToken cancellationToken)
    {
        IsRunning = false;
        return Task.CompletedTask;
    }

    public Task ResumeAsync(CancellationToken cancellationToken)
    {
        IsRunning = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        IsRunning = false;
        return Task.CompletedTask;
    }
}
