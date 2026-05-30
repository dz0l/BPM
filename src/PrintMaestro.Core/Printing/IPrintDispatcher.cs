namespace PrintMaestro.Core.Printing;

public interface IPrintDispatcher
{
    bool IsRunning { get; }

    Task StartAsync(CancellationToken cancellationToken);

    Task PauseAsync(CancellationToken cancellationToken);

    Task ResumeAsync(CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}
