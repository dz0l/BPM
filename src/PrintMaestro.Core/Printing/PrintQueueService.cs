using PrintMaestro.Core.Models;

namespace PrintMaestro.Core.Printing;

public sealed class PrintQueueService : IPrintQueueService
{
    private readonly List<PrintJob> _jobs = [];

    public IReadOnlyList<PrintJob> Jobs => _jobs;

    public event EventHandler? QueueChanged;

    public bool CanAdd(int count = 1) => _jobs.Count + count <= SupportedFileTypes.MaxQueueSize;

    public PrintJob Add(string filePath, PrintSettings? settings = null)
    {
        if (!CanAdd())
        {
            throw new InvalidOperationException($"Очередь не может содержать более {SupportedFileTypes.MaxQueueSize} файлов.");
        }

        if (!SupportedFileTypes.IsSupported(filePath))
        {
            throw new NotSupportedException($"Формат файла не поддерживается: {filePath}");
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Файл не найден.", filePath);
        }

        var job = new PrintJob
        {
            FilePath = filePath,
            Settings = settings?.Clone() ?? new PrintSettings()
        };

        _jobs.Add(job);
        QueueChanged?.Invoke(this, EventArgs.Empty);
        return job;
    }

    public IReadOnlyList<PrintJob> AddRange(IEnumerable<string> filePaths, PrintSettings? settings = null)
    {
        var paths = filePaths.ToList();

        if (!CanAdd(paths.Count))
        {
            throw new InvalidOperationException($"Очередь не может содержать более {SupportedFileTypes.MaxQueueSize} файлов.");
        }

        var added = new List<PrintJob>();

        foreach (var path in paths)
        {
            if (!SupportedFileTypes.IsSupported(path) || !File.Exists(path))
            {
                continue;
            }

            var job = new PrintJob
            {
                FilePath = path,
                Settings = settings?.Clone() ?? new PrintSettings()
            };

            _jobs.Add(job);
            added.Add(job);
        }

        if (added.Count > 0)
        {
            QueueChanged?.Invoke(this, EventArgs.Empty);
        }

        return added;
    }

    public void Remove(Guid jobId)
    {
        var index = _jobs.FindIndex(j => j.Id == jobId);
        if (index < 0)
        {
            return;
        }

        _jobs.RemoveAt(index);
        QueueChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Reorder(int oldIndex, int newIndex)
    {
        if (oldIndex < 0 || oldIndex >= _jobs.Count || newIndex < 0 || newIndex >= _jobs.Count)
        {
            return;
        }

        var job = _jobs[oldIndex];
        _jobs.RemoveAt(oldIndex);
        _jobs.Insert(newIndex, job);
        QueueChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ClearCompleted()
    {
        var removed = _jobs.RemoveAll(j => j.Status == PrintJobStatus.Completed);
        if (removed > 0)
        {
            QueueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void UpdateStatus(Guid jobId, PrintJobStatus status, int progressPercent = 0, string? errorMessage = null)
    {
        var job = _jobs.FirstOrDefault(j => j.Id == jobId);
        if (job is null)
        {
            return;
        }

        job.Status = status;
        job.ProgressPercent = progressPercent;
        job.ErrorMessage = errorMessage;
        QueueChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool HasActiveJobs => _jobs.Any(j => j.Status is
        PrintJobStatus.Preparing or
        PrintJobStatus.Dispatching or
        PrintJobStatus.Spooled or
        PrintJobStatus.Printing or
        PrintJobStatus.RetryWaiting);

    public void PauseJob(Guid jobId)
    {
        var job = _jobs.FirstOrDefault(j => j.Id == jobId);
        if (job is null)
        {
            return;
        }

        if (job.Status is PrintJobStatus.Pending or PrintJobStatus.Printing or PrintJobStatus.Preparing)
        {
            job.Status = PrintJobStatus.Paused;
            QueueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ResumeJob(Guid jobId)
    {
        var job = _jobs.FirstOrDefault(j => j.Id == jobId);
        if (job is null)
        {
            return;
        }

        if (job.Status == PrintJobStatus.Paused)
        {
            job.Status = PrintJobStatus.Pending;
            QueueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void RetryJob(Guid jobId)
    {
        var job = _jobs.FirstOrDefault(j => j.Id == jobId);
        if (job is null)
        {
            return;
        }

        if (job.Status is PrintJobStatus.Failed or PrintJobStatus.Canceled)
        {
            job.Status = PrintJobStatus.Pending;
            job.ErrorMessage = null;
            job.ProgressPercent = 0;
            QueueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void CancelJob(Guid jobId)
    {
        var job = _jobs.FirstOrDefault(j => j.Id == jobId);
        if (job is null)
        {
            return;
        }

        if (job.Status is PrintJobStatus.Completed)
        {
            return;
        }

        job.Status = PrintJobStatus.Canceled;
        job.ProgressPercent = 0;
        QueueChanged?.Invoke(this, EventArgs.Empty);
    }
}
