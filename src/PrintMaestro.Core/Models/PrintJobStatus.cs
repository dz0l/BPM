namespace PrintMaestro.Core.Models;

public enum PrintJobStatus
{
    Pending,
    Preparing,
    Dispatching,
    Spooled,
    Printing,
    Completed,
    Failed,
    Paused,
    Canceled,
    RetryWaiting
}
