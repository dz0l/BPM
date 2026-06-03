using System.Diagnostics;
using PrintMaestro.Core.Configuration;
using PrintMaestro.Core.IPC;
using PrintMaestro.Core.Printing;
using Serilog;

namespace PrintMaestro.Infrastructure.Workers;

public enum WorkerKind
{
    Pdf,
    Image,
    Office
}

public interface IWorkerPrintService : IAsyncDisposable
{
    Task<PrintResult> PrintAsync(WorkerKind kind, PrintRequest request, CancellationToken cancellationToken);
}

public sealed class WorkerPrintService : IWorkerPrintService
{
    private readonly WorkerProcessHost _pdfHost;
    private readonly WorkerProcessHost _imageHost;
    private readonly WorkerProcessHost _officeHost;
    private readonly ISettingsStore _settingsStore;

    public WorkerPrintService(ISettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
        _pdfHost = new WorkerProcessHost(
            WorkerKind.Pdf,
            WorkerPipeNames.Pdf,
            "PrintMaestro.PdfWorker.exe",
            Path.Combine("workers", "pdf"));

        _imageHost = new WorkerProcessHost(
            WorkerKind.Image,
            WorkerPipeNames.Image,
            "PrintMaestro.ImageWorker.exe",
            Path.Combine("workers", "image"));

        _officeHost = new WorkerProcessHost(
            WorkerKind.Office,
            WorkerPipeNames.Office,
            "PrintMaestro.OfficeWorker.exe",
            Path.Combine("workers", "office"));
    }

    public async Task<PrintResult> PrintAsync(WorkerKind kind, PrintRequest request, CancellationToken cancellationToken)
    {
        var host = kind switch
        {
            WorkerKind.Pdf => _pdfHost,
            WorkerKind.Image => _imageHost,
            WorkerKind.Office => _officeHost,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        var pdfRenderDpi = PdfRenderDpiOptions.Default;
        if (kind == WorkerKind.Pdf)
        {
            var appSettings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
            pdfRenderDpi = appSettings.PdfRenderDpi;
        }

        return await host.PrintAsync(request, pdfRenderDpi, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        await _pdfHost.DisposeAsync().ConfigureAwait(false);
        await _imageHost.DisposeAsync().ConfigureAwait(false);
        await _officeHost.DisposeAsync().ConfigureAwait(false);
    }
}

internal sealed class WorkerProcessHost : IAsyncDisposable
{
    private readonly WorkerKind _kind;
    private readonly string _pipeName;
    private readonly string _executableName;
    private readonly string _workingDirectory;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private Process? _process;

    public WorkerProcessHost(WorkerKind kind, string pipeName, string executableName, string workingDirectory)
    {
        _kind = kind;
        _pipeName = pipeName;
        _executableName = executableName;
        _workingDirectory = workingDirectory;
    }

    public async Task<PrintResult> PrintAsync(PrintRequest request, int pdfRenderDpi, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await EnsureStartedAsync(cancellationToken).ConfigureAwait(false);

            var message = new WorkerMessage
            {
                Command = WorkerCommandType.Print,
                JobId = request.JobId,
                FilePath = request.FilePath,
                Settings = request.Settings,
                PdfRenderDpi = _kind == WorkerKind.Pdf
                    ? pdfRenderDpi
                    : PdfRenderDpiOptions.Default
            };

            var response = await WorkerPipeHost.SendRequestAsync(
                _pipeName,
                message,
                WorkerIpcDefaults.ConnectTimeout,
                WorkerIpcDefaults.ResponseTimeout,
                cancellationToken).ConfigureAwait(false);

            return response.Success
                ? PrintResult.Ok()
                : PrintResult.Fail(response.ErrorCode ?? "WORKER_FAILED", response.ErrorMessage ?? "Worker failed.", response.IsRetryable);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "{WorkerKind} worker print failed.", _kind);
            return PrintResult.Fail("WORKER_ERROR", ex.Message, retryable: false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task EnsureStartedAsync(CancellationToken cancellationToken)
    {
        if (_process is { HasExited: false })
        {
            return;
        }

        var exePath = Path.Combine(AppContext.BaseDirectory, _workingDirectory, _executableName);
        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException($"Worker executable not found: {exePath}");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = Path.GetDirectoryName(exePath)!,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        _process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start {_kind} worker.");

        using var startupCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        startupCts.CancelAfter(WorkerIpcDefaults.StartupTimeout);

        var deadline = DateTime.UtcNow + WorkerIpcDefaults.StartupTimeout;

        while (DateTime.UtcNow < deadline && !startupCts.IsCancellationRequested)
        {
            if (_process.HasExited)
            {
                throw new InvalidOperationException($"{_kind} worker exited during startup.");
            }

            if (await PingWorkerAsync(_pipeName, startupCts.Token).ConfigureAwait(false))
            {
                Log.Information("{WorkerKind} worker started (PID {ProcessId}).", _kind, _process.Id);
                return;
            }

            await Task.Delay(200, startupCts.Token).ConfigureAwait(false);
        }

        throw new TimeoutException($"{_kind} worker startup timed out.");
    }

    private static async Task<bool> PingWorkerAsync(string pipeName, CancellationToken cancellationToken)
    {
        try
        {
            var response = await WorkerPipeHost.SendRequestAsync(
                pipeName,
                new WorkerMessage { Command = WorkerCommandType.Ping },
                WorkerIpcDefaults.ConnectTimeout,
                WorkerIpcDefaults.ResponseTimeout,
                cancellationToken).ConfigureAwait(false);

            return response.Success;
        }
        catch
        {
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_process is { HasExited: false })
        {
            try
            {
                await WorkerPipeHost.SendRequestAsync(
                    _pipeName,
                    new WorkerMessage { Command = WorkerCommandType.Shutdown },
                    WorkerIpcDefaults.ConnectTimeout,
                    WorkerIpcDefaults.ResponseTimeout,
                    CancellationToken.None).ConfigureAwait(false);
            }
            catch
            {
                // Best effort shutdown via IPC.
            }

            if (!_process.WaitForExit(3000))
            {
                _process.Kill(entireProcessTree: true);
            }
        }

        _process?.Dispose();
        _process = null;
        _gate.Dispose();
    }
}
