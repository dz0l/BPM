using PrintMaestro.Core.Configuration;
using PrintMaestro.Core.History;
using PrintMaestro.Core.Models;
using PrintMaestro.Core.Printing;
using Serilog;

namespace PrintMaestro.Infrastructure.Printing;

public sealed class ControlledSequentialPrintDispatcher : IPrintDispatcher
{
    private readonly IPrintQueueService _queueService;
    private readonly PrintDocumentHandlerRegistry _handlerRegistry;
    private readonly ISettingsStore _settingsStore;
    private readonly IPrintHistoryRepository _historyRepository;

    private CancellationTokenSource? _runCts;
    private CancellationTokenSource? _currentJobCts;
    private Task? _runTask;
    private volatile bool _paused;

    public ControlledSequentialPrintDispatcher(
        IPrintQueueService queueService,
        PrintDocumentHandlerRegistry handlerRegistry,
        ISettingsStore settingsStore,
        IPrintHistoryRepository historyRepository)
    {
        _queueService = queueService;
        _handlerRegistry = handlerRegistry;
        _settingsStore = settingsStore;
        _historyRepository = historyRepository;
    }

    public bool IsRunning { get; private set; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            return _runTask ?? Task.CompletedTask;
        }

        _paused = false;
        IsRunning = true;
        _runCts = new CancellationTokenSource();
        _runTask = RunLoopAsync(_runCts.Token);
        return _runTask;
    }

    public Task PauseAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _paused = true;
        return Task.CompletedTask;
    }

    public Task ResumeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _paused = false;
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsRunning)
        {
            return;
        }

        _runCts?.Cancel();
        CancelCurrentJob();

        if (_runTask is not null)
        {
            try
            {
                await _runTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping the dispatcher.
            }
        }

        IsRunning = false;
        _runTask = null;
        _runCts?.Dispose();
        _runCts = null;
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
            var errorPolicy = settings.OnPrintError;
            var jobTimeout = TimeSpan.FromSeconds(Math.Max(30, settings.PrintJobTimeoutSeconds));

            while (!cancellationToken.IsCancellationRequested)
            {
                await WaitWhilePausedAsync(cancellationToken).ConfigureAwait(false);

                var job = GetNextPendingJob();
                if (job is null)
                {
                    break;
                }

                var shouldContinue = await ProcessJobAsync(job, errorPolicy, jobTimeout, cancellationToken)
                    .ConfigureAwait(false);

                if (!shouldContinue)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Log.Information("Print dispatcher stopped.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Print dispatcher loop failed.");
        }
        finally
        {
            IsRunning = false;
            _runTask = null;
        }
    }

    private async Task<bool> ProcessJobAsync(
        PrintJob job,
        PrintErrorPolicy errorPolicy,
        TimeSpan jobTimeout,
        CancellationToken cancellationToken)
    {
        if (job.Status is PrintJobStatus.Canceled or PrintJobStatus.Paused)
        {
            return true;
        }

        var kind = DocumentKindResolver.Resolve(job.FilePath);
        var handler = _handlerRegistry.GetHandler(kind);
        if (handler is null || kind == DocumentKind.Unknown)
        {
            _queueService.UpdateStatus(job.Id, PrintJobStatus.Failed, errorMessage: "Unsupported document type.");
            await RecordHistoryAsync(job, DateTimeOffset.UtcNow, false, "Unsupported document type.", cancellationToken)
                .ConfigureAwait(false);
            return ApplyErrorPolicy(errorPolicy, job);
        }

        var startTime = DateTimeOffset.UtcNow;

        var request = new PrintRequest
        {
            JobId = job.Id,
            FilePath = job.FilePath,
            Settings = job.Settings,
            DocumentKind = kind
        };

        _queueService.UpdateStatus(job.Id, PrintJobStatus.Preparing, progressPercent: 5);
        _queueService.UpdateStatus(job.Id, PrintJobStatus.Dispatching, progressPercent: 10);

        PrintResult? lastResult = null;

        for (var attempt = 0; attempt < RetryPolicy.MaxAttempts; attempt++)
        {
            if (ShouldAbortJob(job, cancellationToken))
            {
                return true;
            }

            if (attempt > 0)
            {
                job.RetryCount = attempt;
                _queueService.UpdateStatus(job.Id, PrintJobStatus.RetryWaiting, progressPercent: 0);
                await DelayWithJobWatchAsync(job, RetryPolicy.GetBackoffDelay(attempt - 1), cancellationToken)
                    .ConfigureAwait(false);

                if (ShouldAbortJob(job, cancellationToken))
                {
                    return true;
                }
            }

            _queueService.UpdateStatus(job.Id, PrintJobStatus.Printing, progressPercent: 25);

            using var jobCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            jobCts.CancelAfter(jobTimeout);
            _currentJobCts = jobCts;

            try
            {
                using var jobMonitorCts = CancellationTokenSource.CreateLinkedTokenSource(jobCts.Token);
                var monitorTask = MonitorJobStateAsync(job, jobMonitorCts.Token);

                lastResult = await handler.PrintAsync(request, jobCts.Token).ConfigureAwait(false);

                jobMonitorCts.Cancel();
                try
                {
                    await monitorTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Monitor cancelled after print finished.
                }

                if (ShouldAbortJob(job, cancellationToken))
                {
                    return true;
                }

                if (lastResult.Success)
                {
                    _queueService.UpdateStatus(job.Id, PrintJobStatus.Completed, progressPercent: 100);
                    Log.Information("Print job completed: {FilePath}", job.FilePath);
                    await RecordHistoryAsync(job, startTime, true, null, cancellationToken).ConfigureAwait(false);
                    return true;
                }

                if (!lastResult.IsRetryable || errorPolicy != PrintErrorPolicy.Retry)
                {
                    break;
                }
            }
            catch (OperationCanceledException) when (jobCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                lastResult = PrintResult.Fail("WATCHDOG_TIMEOUT", "Print job timed out.", retryable: true);

                if (job.Status == PrintJobStatus.Canceled)
                {
                    return true;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (job.Status is not PrintJobStatus.Completed and not PrintJobStatus.Canceled)
                {
                    _queueService.UpdateStatus(job.Id, PrintJobStatus.Canceled);
                }

                return false;
            }
            catch (Exception ex)
            {
                lastResult = PrintResult.Fail("PRINT_EXCEPTION", ex.Message);
                Log.Error(ex, "Print job failed with exception: {FilePath}", job.FilePath);
                break;
            }
            finally
            {
                if (ReferenceEquals(_currentJobCts, jobCts))
                {
                    _currentJobCts = null;
                }
            }

            if (errorPolicy != PrintErrorPolicy.Retry || attempt == RetryPolicy.MaxAttempts - 1)
            {
                break;
            }
        }

        var message = lastResult?.ErrorMessage ?? "Print failed.";
        _queueService.UpdateStatus(job.Id, PrintJobStatus.Failed, errorMessage: message);
        Log.Warning("Print job failed: {FilePath} — {Message}", job.FilePath, message);
        await RecordHistoryAsync(job, startTime, false, message, cancellationToken).ConfigureAwait(false);

        return ApplyErrorPolicy(errorPolicy, job);
    }

    private async Task RecordHistoryAsync(
        PrintJob job,
        DateTimeOffset startTime,
        bool success,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        try
        {
            await _historyRepository.AddAsync(
                new PrintHistoryEntry
                {
                    FileName = Path.GetFileName(job.FilePath),
                    FilePath = job.FilePath,
                    PrinterName = job.Settings.PrinterName,
                    UserName = Environment.UserName,
                    StartTime = startTime,
                    EndTime = DateTimeOffset.UtcNow,
                    Success = success,
                    ErrorMessage = errorMessage,
                    Copies = job.Settings.Copies
                },
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to record print history for {FilePath}", job.FilePath);
        }
    }

    private bool ApplyErrorPolicy(PrintErrorPolicy errorPolicy, PrintJob job)
    {
        return errorPolicy switch
        {
            PrintErrorPolicy.CancelQueue => false,
            PrintErrorPolicy.Skip => true,
            PrintErrorPolicy.Retry => true,
            _ => true
        };
    }

    private PrintJob? GetNextPendingJob() =>
        _queueService.Jobs.FirstOrDefault(j => j.Status == PrintJobStatus.Pending);

    private async Task WaitWhilePausedAsync(CancellationToken cancellationToken)
    {
        while (_paused && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(200, cancellationToken).ConfigureAwait(false);
        }
    }

    private bool ShouldAbortJob(PrintJob job, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            if (job.Status is not PrintJobStatus.Completed and not PrintJobStatus.Canceled)
            {
                _queueService.UpdateStatus(job.Id, PrintJobStatus.Canceled);
            }

            return true;
        }

        return job.Status is PrintJobStatus.Canceled or PrintJobStatus.Paused;
    }

    private async Task DelayWithJobWatchAsync(PrintJob job, TimeSpan delay, CancellationToken cancellationToken)
    {
        var elapsed = TimeSpan.Zero;
        var step = TimeSpan.FromMilliseconds(200);

        while (elapsed < delay)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (job.Status is PrintJobStatus.Canceled or PrintJobStatus.Paused)
            {
                return;
            }

            var remaining = delay - elapsed;
            var wait = remaining < step ? remaining : step;
            await Task.Delay(wait, cancellationToken).ConfigureAwait(false);
            elapsed += wait;
        }
    }

    private async Task MonitorJobStateAsync(PrintJob job, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (job.Status is PrintJobStatus.Canceled or PrintJobStatus.Paused)
            {
                CancelCurrentJob();
                return;
            }

            await Task.Delay(250, cancellationToken).ConfigureAwait(false);
        }
    }

    private void CancelCurrentJob() => _currentJobCts?.Cancel();
}
