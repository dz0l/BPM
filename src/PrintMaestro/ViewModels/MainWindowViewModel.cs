using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GongSolutions.Wpf.DragDrop;
using PrintMaestro.Core.Configuration;
using PrintMaestro.Core.IO;
using PrintMaestro.Core.Models;
using PrintMaestro.Core.Printing;
using PrintMaestro.Core.Printers;
using PrintMaestro.Core.Updates;
using PrintMaestro.Services;

namespace PrintMaestro.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject, IDropTarget, ICurrentPrintSettingsSource
{
    private readonly IPrintQueueService _queueService;
    private readonly IPrintDispatcher _printDispatcher;
    private readonly IPrinterDiscoveryService _printerDiscovery;
    private readonly IFileDialogService _fileDialogService;
    private readonly ISettingsStore _settingsStore;
    private readonly IUpdateService _updateService;
    private readonly ILocalizationService _localization;
    private readonly ISettingsDialogService _settingsDialogService;
    private readonly IHistoryDialogService _historyDialogService;
    private readonly INotificationService _notificationService;
    private readonly IThumbnailService _thumbnailService;
    private readonly IDialogService _dialogService;

    private bool _isApplyingProfile;

    public MainWindowViewModel(
        IPrintQueueService queueService,
        IPrintDispatcher printDispatcher,
        IPrinterDiscoveryService printerDiscovery,
        IFileDialogService fileDialogService,
        ISettingsStore settingsStore,
        IUpdateService updateService,
        ILocalizationService localization,
        ISettingsDialogService settingsDialogService,
        IHistoryDialogService historyDialogService,
        INotificationService notificationService,
        IThumbnailService thumbnailService,
        IDialogService dialogService)
    {
        _queueService = queueService;
        _printDispatcher = printDispatcher;
        _printerDiscovery = printerDiscovery;
        _fileDialogService = fileDialogService;
        _settingsStore = settingsStore;
        _updateService = updateService;
        _localization = localization;
        _settingsDialogService = settingsDialogService;
        _historyDialogService = historyDialogService;
        _notificationService = notificationService;
        _thumbnailService = thumbnailService;
        _dialogService = dialogService;

        _queueService.QueueChanged += (_, _) =>
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher is null || dispatcher.CheckAccess())
            {
                RefreshQueue();
                return;
            }

            dispatcher.InvokeAsync(RefreshQueue);
        };
        _localization.CultureChanged += (_, _) => RefreshLocalizedProperties();

        StatusText = _localization.GetString("Status.Ready");

        RefreshQueue();
    }

    public ObservableCollection<PrintJobViewModel> Jobs { get; } = [];

    public ObservableCollection<string> Printers { get; } = [];

    public ObservableCollection<PrintProfileItemViewModel> PrintProfiles { get; } = [];

    [ObservableProperty]
    private PrintProfileItemViewModel? _selectedPrintProfile;

    [ObservableProperty]
    private string _selectedPrinter = string.Empty;

    [ObservableProperty]
    private PaperFormat _paperFormat = PaperFormat.A4;

    [ObservableProperty]
    private PaperOrientation _orientation = PaperOrientation.Auto;

    [ObservableProperty]
    private int _copies = 1;

    [ObservableProperty]
    private bool _duplexEnabled;

    [ObservableProperty]
    private bool _insertBlankPageAfter;

    [ObservableProperty]
    private bool _isPrinting;

    [ObservableProperty]
    private bool _isPaused;

    public bool ShowStartPrint => !IsPrinting;

    public bool ShowResumePrint => IsPrinting && IsPaused;

    public bool ShowPausePrint => IsPrinting && !IsPaused;

    public bool ShowCancelPrint => IsPrinting;

    partial void OnIsPrintingChanged(bool value) => NotifyPrintControlsChanged();

    partial void OnIsPausedChanged(bool value) => NotifyPrintControlsChanged();

    private void NotifyPrintControlsChanged()
    {
        OnPropertyChanged(nameof(ShowStartPrint));
        OnPropertyChanged(nameof(ShowResumePrint));
        OnPropertyChanged(nameof(ShowPausePrint));
        OnPropertyChanged(nameof(ShowCancelPrint));
    }

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _completedCount;

    [ObservableProperty]
    private int _failedCount;

    public bool ShouldConfirmShutdown() => IsPrinting || _queueService.HasActiveJobs;

    public void AddDroppedPaths(IEnumerable<string> paths)
    {
        var collection = DroppedPathCollector.Collect(paths);
        AddFiles(collection.SupportedFiles, collection.SkippedCount);
    }

    void IDropTarget.DragOver(IDropInfo dropInfo)
    {
        if (dropInfo.Data is PrintJobViewModel)
        {
            dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
            dropInfo.Effects = System.Windows.DragDropEffects.Move;
            return;
        }

        dropInfo.Effects = System.Windows.DragDropEffects.None;
    }

    void IDropTarget.Drop(IDropInfo dropInfo)
    {
        if (dropInfo.Data is not PrintJobViewModel sourceItem)
        {
            return;
        }

        var oldIndex = Jobs.IndexOf(sourceItem);
        if (oldIndex < 0)
        {
            return;
        }

        var newIndex = dropInfo.InsertIndex;
        if (newIndex > Jobs.Count)
        {
            newIndex = Jobs.Count;
        }

        if (oldIndex < newIndex)
        {
            newIndex--;
        }

        if (oldIndex == newIndex)
        {
            return;
        }

        _queueService.Reorder(oldIndex, newIndex);
        _notificationService.ShowSuccess(_localization.GetString("Toast.QueueReordered"));
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken);
        PaperFormat = settings.DefaultPrintSettings.PaperFormat;
        Orientation = settings.DefaultPrintSettings.Orientation;
        Copies = settings.DefaultPrintSettings.Copies;
        DuplexEnabled = settings.DefaultPrintSettings.Duplex != DuplexMode.Simplex;
        InsertBlankPageAfter = settings.DefaultPrintSettings.InsertBlankPageAfter;

        var printers = await _printerDiscovery.GetPrintersAsync(cancellationToken);
        Printers.Clear();

        foreach (var printer in printers)
        {
            Printers.Add(printer.Name);
        }

        SelectedPrinter = settings.DefaultPrinterName;
        if (string.IsNullOrWhiteSpace(SelectedPrinter))
        {
            SelectedPrinter = printers.FirstOrDefault(p => p.IsDefault)?.Name
                ?? printers.FirstOrDefault()?.Name
                ?? string.Empty;
        }

        if (settings.CheckUpdatesOnStartup)
        {
            _ = CheckUpdatesAsync(cancellationToken);
        }

        await LoadPrintProfilesAsync(settings, cancellationToken);
    }

    partial void OnSelectedPrintProfileChanged(PrintProfileItemViewModel? value)
    {
        if (value is null || _isApplyingProfile)
        {
            return;
        }

        ApplyPrintSettings(value.Settings);
    }

    [RelayCommand]
    private void OpenSettings()
    {
        if (System.Windows.Application.Current.MainWindow is System.Windows.Window owner)
        {
            _settingsDialogService.ShowSettings(owner);
            _ = RefreshPrintProfilesAsync();
        }
    }

    [RelayCommand]
    private void OpenHistory()
    {
        if (System.Windows.Application.Current.MainWindow is System.Windows.Window owner)
        {
            _historyDialogService.ShowHistory(owner);
        }
    }

    public PrintSettings CaptureCurrentPrintSettings() => CreateCurrentSettings();

    public async Task RefreshPrintProfilesAsync()
    {
        var settings = await _settingsStore.LoadAsync(CancellationToken.None);
        await LoadPrintProfilesAsync(settings, CancellationToken.None);
    }

    private async Task LoadPrintProfilesAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        PrintProfiles.Clear();

        foreach (var profile in settings.PrintProfiles.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase))
        {
            PrintProfiles.Add(new PrintProfileItemViewModel(profile));
        }

        await Task.CompletedTask;
    }

    private void ApplyPrintSettings(PrintSettings settings)
    {
        _isApplyingProfile = true;
        try
        {
            SelectedPrinter = settings.PrinterName;
            PaperFormat = settings.PaperFormat;
            Orientation = settings.Orientation;
            Copies = settings.Copies;
            DuplexEnabled = settings.Duplex != DuplexMode.Simplex;
            InsertBlankPageAfter = settings.InsertBlankPageAfter;
        }
        finally
        {
            _isApplyingProfile = false;
        }
    }

    [RelayCommand]
    private void AddDocuments()
    {
        var files = _fileDialogService.OpenDocuments();
        AddFiles(files);
    }

    [RelayCommand]
    private void AddFolder()
    {
        var folder = _fileDialogService.PickFolder();
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        var collection = DroppedPathCollector.Collect([folder]);
        AddFiles(collection.SupportedFiles, collection.SkippedCount);
    }

    [RelayCommand]
    private async Task StartPrintAsync(CancellationToken cancellationToken)
    {
        if (Jobs.Count == 0)
        {
            StatusText = _localization.GetString("Status.AddDocumentsHint");
            _notificationService.ShowWarning(_localization.GetString("Status.AddDocumentsHint"));
            return;
        }

        if (_printDispatcher.IsRunning)
        {
            await _printDispatcher.ResumeAsync(CancellationToken.None);
            IsPaused = false;
            StatusText = _localization.GetString("Status.PrintStarted");
            _notificationService.ShowSuccess(_localization.GetString("Toast.PrintResumed"));
            return;
        }

        ResetInterruptedJobs();
        PrepareCompletedJobsForReprint();
        ApplyCurrentSettingsToPendingJobs();
        await PersistPanelSettingsAsync();

        IsPrinting = true;
        IsPaused = false;
        StatusText = _localization.GetString("Status.PrintStarted");

        try
        {
            _notificationService.ShowSuccess(_localization.GetString("Toast.PrintStarted"));
            await _printDispatcher.StartAsync(CancellationToken.None);
            RefreshQueue();
            StatusText = _localization.GetString("Status.PrintCompleted");
            _notificationService.ShowSuccess(_localization.GetString("Status.PrintCompleted"));
        }
        catch (OperationCanceledException)
        {
            StatusText = _localization.GetString("Status.PrintStopped");
        }
        finally
        {
            IsPrinting = false;
            IsPaused = false;
        }
    }

    [RelayCommand]
    private async Task PausePrintAsync()
    {
        if (!IsPrinting)
        {
            return;
        }

        await _printDispatcher.PauseAsync(CancellationToken.None);
        IsPaused = true;
        StatusText = _localization.GetString("Status.PrintPaused");
        _notificationService.ShowWarning(_localization.GetString("Toast.PrintPaused"));
    }

    [RelayCommand]
    private async Task CancelPrintAsync()
    {
        if (!IsPrinting)
        {
            return;
        }

        await StopPrintingAsync();
        StatusText = _localization.GetString("Status.PrintStopped");
        _notificationService.ShowWarning(_localization.GetString("Toast.PrintCanceled"));
    }

    public async Task StopPrintingAsync(CancellationToken cancellationToken = default)
    {
        if (!_printDispatcher.IsRunning)
        {
            return;
        }

        await _printDispatcher.StopAsync(cancellationToken);
        IsPrinting = false;
        IsPaused = false;
        RefreshQueue();
    }

    [RelayCommand]
    private void ClearCompleted()
    {
        _queueService.ClearCompleted();
        RefreshQueue();
        StatusText = _localization.GetString("Status.CompletedCleared");
        _notificationService.ShowSuccess(_localization.GetString("Status.CompletedCleared"));
    }

    [RelayCommand]
    private void RemoveJob(PrintJobViewModel? job)
    {
        if (job is null)
        {
            return;
        }

        _queueService.Remove(job.Id);
        RefreshQueue();
        _notificationService.ShowSuccess(_localization.GetString("Toast.JobRemoved"));
    }

    [RelayCommand]
    private void PauseJob(PrintJobViewModel? job)
    {
        if (job is null)
        {
            return;
        }

        _queueService.PauseJob(job.Id);
        _notificationService.ShowWarning(_localization.GetString("Toast.JobPaused"));
    }

    [RelayCommand]
    private void ResumeJob(PrintJobViewModel? job)
    {
        if (job is null)
        {
            return;
        }

        _queueService.ResumeJob(job.Id);
        _notificationService.ShowSuccess(_localization.GetString("Toast.JobResumed"));
    }

    [RelayCommand]
    private void RetryJob(PrintJobViewModel? job)
    {
        if (job is null)
        {
            return;
        }

        _queueService.RetryJob(job.Id);
        _notificationService.ShowSuccess(_localization.GetString("Toast.JobRetry"));
    }

    [RelayCommand]
    private void CancelJob(PrintJobViewModel? job)
    {
        if (job is null)
        {
            return;
        }

        _queueService.CancelJob(job.Id);
        _notificationService.ShowWarning(_localization.GetString("Toast.JobCanceled"));
    }

    private void RefreshLocalizedProperties()
    {
        StatusText = _localization.GetString("Status.Ready");
    }

    private void AddFiles(IReadOnlyList<string> files, int skippedCount = 0)
    {
        if (files.Count == 0 && skippedCount == 0)
        {
            _notificationService.ShowWarning(_localization.GetString("Toast.NoSupportedFiles"));
            return;
        }

        try
        {
            var settings = CreateCurrentSettings();
            var added = _queueService.AddRange(files, settings);
            RefreshQueue();

            if (added.Count == 0)
            {
                if (skippedCount > 0)
                {
                    _notificationService.ShowWarning(_localization.GetString("Toast.FilesSkipped", skippedCount));
                }
                else
                {
                    _notificationService.ShowWarning(_localization.GetString("Toast.NoSupportedFiles"));
                }

                return;
            }

            StatusText = _localization.GetString("Status.FilesAdded", added.Count);
            _notificationService.ShowSuccess(_localization.GetString("Status.FilesAdded", added.Count));

            if (skippedCount > 0)
            {
                _notificationService.ShowWarning(_localization.GetString("Toast.FilesSkipped", skippedCount));
            }
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
            _notificationService.ShowError(ex.Message);
        }
    }

    private async Task PersistPanelSettingsAsync()
    {
        var settings = await _settingsStore.LoadAsync(CancellationToken.None);
        settings.DefaultPrinterName = SelectedPrinter;
        settings.DefaultPrintSettings = CreateCurrentSettings();
        await _settingsStore.SaveAsync(settings, CancellationToken.None);
    }

    private PrintSettings CreateCurrentSettings() => new()
    {
        PrinterName = SelectedPrinter,
        PaperFormat = PaperFormat,
        Orientation = Orientation,
        Copies = Copies,
        Duplex = DuplexEnabled ? DuplexMode.DuplexLongEdge : DuplexMode.Simplex,
        AutoDetectLayout = false,
        FitToPage = true,
        InsertBlankPageAfter = InsertBlankPageAfter
    };

    private void PrepareCompletedJobsForReprint()
    {
        foreach (var job in _queueService.Jobs.Where(j => j.Status == PrintJobStatus.Completed))
        {
            _queueService.UpdateStatus(job.Id, PrintJobStatus.Pending, progressPercent: 0, errorMessage: null);
        }
    }

    private void ApplyCurrentSettingsToPendingJobs()
    {
        var settings = CreateCurrentSettings();

        foreach (var job in _queueService.Jobs.Where(j => j.Status == PrintJobStatus.Pending))
        {
            job.Settings = settings.Clone();
        }
    }

    private void ResetInterruptedJobs()
    {
        foreach (var job in _queueService.Jobs)
        {
            if (job.Status is PrintJobStatus.Preparing or PrintJobStatus.Dispatching
                or PrintJobStatus.Printing or PrintJobStatus.RetryWaiting)
            {
                _queueService.UpdateStatus(job.Id, PrintJobStatus.Pending, progressPercent: 0);
            }
        }
    }

    private void RefreshQueue()
    {
        var serviceJobs = _queueService.Jobs;
        var existing = Jobs.ToDictionary(j => j.Id);

        for (var index = 0; index < serviceJobs.Count; index++)
        {
            var job = serviceJobs[index];

            if (index < Jobs.Count && Jobs[index].Id == job.Id)
            {
                Jobs[index].SyncFrom(job);
                continue;
            }

            if (existing.TryGetValue(job.Id, out var knownJob))
            {
                knownJob.SyncFrom(job);
                Jobs.Remove(knownJob);
                Jobs.Insert(index, knownJob);
                continue;
            }

            Jobs.Insert(index, new PrintJobViewModel(job, _localization, _thumbnailService));
        }

        while (Jobs.Count > serviceJobs.Count)
        {
            Jobs.RemoveAt(Jobs.Count - 1);
        }

        TotalCount = Jobs.Count;
        CompletedCount = Jobs.Count(j => j.Status == PrintJobStatus.Completed);
        FailedCount = Jobs.Count(j => j.Status == PrintJobStatus.Failed);
    }

    private async Task CheckUpdatesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var update = await _updateService.CheckForUpdateAsync(cancellationToken);
            if (update is not null)
            {
                StatusText = _localization.GetString("Status.UpdateAvailable", update.Version);
                _notificationService.ShowSuccess(_localization.GetString("Status.UpdateAvailable", update.Version));

                if (_dialogService.ConfirmInstallUpdate(update.Version.ToString(3)))
                {
                    await _updateService.DownloadAndApplyAsync(update, cancellationToken);
                }
            }
        }
        catch
        {
            // Offline-first: network errors must not block startup.
        }
    }
}
