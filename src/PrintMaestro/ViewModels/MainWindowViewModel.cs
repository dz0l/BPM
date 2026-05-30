using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrintMaestro.Core.Configuration;
using PrintMaestro.Core.Models;
using PrintMaestro.Core.Printing;
using PrintMaestro.Core.Printers;
using PrintMaestro.Core.Updates;
using PrintMaestro.Services;

namespace PrintMaestro.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IPrintQueueService _queueService;
    private readonly IPrintDispatcher _printDispatcher;
    private readonly IPrinterDiscoveryService _printerDiscovery;
    private readonly IFileDialogService _fileDialogService;
    private readonly ISettingsStore _settingsStore;
    private readonly IUpdateService _updateService;
    private readonly ILocalizationService _localization;
    private readonly ISettingsDialogService _settingsDialogService;

    public MainWindowViewModel(
        IPrintQueueService queueService,
        IPrintDispatcher printDispatcher,
        IPrinterDiscoveryService printerDiscovery,
        IFileDialogService fileDialogService,
        ISettingsStore settingsStore,
        IUpdateService updateService,
        ILocalizationService localization,
        ISettingsDialogService settingsDialogService)
    {
        _queueService = queueService;
        _printDispatcher = printDispatcher;
        _printerDiscovery = printerDiscovery;
        _fileDialogService = fileDialogService;
        _settingsStore = settingsStore;
        _updateService = updateService;
        _localization = localization;
        _settingsDialogService = settingsDialogService;

        _queueService.QueueChanged += (_, _) => RefreshQueue();
        _localization.CultureChanged += (_, _) => RefreshLocalizedProperties();

        StatusText = _localization.GetString("Status.Ready");

        RefreshQueue();
    }

    public ObservableCollection<PrintJobViewModel> Jobs { get; } = [];

    public ObservableCollection<string> Printers { get; } = [];

    [ObservableProperty]
    private string _selectedPrinter = string.Empty;

    [ObservableProperty]
    private PaperFormat _paperFormat = PaperFormat.A4;

    [ObservableProperty]
    private PaperOrientation _orientation = PaperOrientation.Portrait;

    [ObservableProperty]
    private int _copies = 1;

    [ObservableProperty]
    private bool _duplexEnabled;

    [ObservableProperty]
    private bool _isPrinting;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _completedCount;

    [ObservableProperty]
    private int _failedCount;

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken);
        PaperFormat = settings.DefaultPrintSettings.PaperFormat;
        Orientation = settings.DefaultPrintSettings.Orientation;
        Copies = settings.DefaultPrintSettings.Copies;
        DuplexEnabled = settings.DefaultPrintSettings.Duplex != DuplexMode.Simplex;

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
    }

    [RelayCommand]
    private void OpenSettings()
    {
        if (System.Windows.Application.Current.MainWindow is System.Windows.Window owner)
        {
            _settingsDialogService.ShowSettings(owner);
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
        var files = _fileDialogService.OpenFolder();
        AddFiles(files);
    }

    [RelayCommand]
    private async Task StartPrintAsync(CancellationToken cancellationToken)
    {
        if (Jobs.Count == 0)
        {
            StatusText = _localization.GetString("Status.AddDocumentsHint");
            return;
        }

        IsPrinting = true;
        StatusText = _localization.GetString("Status.PrintStartedMvp");
        await _printDispatcher.StartAsync(cancellationToken);
    }

    [RelayCommand]
    private async Task PausePrintAsync(CancellationToken cancellationToken)
    {
        await _printDispatcher.PauseAsync(cancellationToken);
        IsPrinting = false;
        StatusText = _localization.GetString("Status.PrintPaused");
    }

    [RelayCommand]
    private void ClearCompleted()
    {
        _queueService.ClearCompleted();
        RefreshQueue();
        StatusText = _localization.GetString("Status.CompletedCleared");
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
    }

    private void RefreshLocalizedProperties()
    {
        StatusText = _localization.GetString("Status.Ready");
    }

    private void AddFiles(IReadOnlyList<string> files)
    {
        if (files.Count == 0)
        {
            return;
        }

        try
        {
            var settings = CreateCurrentSettings();
            _queueService.AddRange(files, settings);
            RefreshQueue();
            StatusText = _localization.GetString("Status.FilesAdded", files.Count);
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
        }
    }

    private PrintSettings CreateCurrentSettings() => new()
    {
        PrinterName = SelectedPrinter,
        PaperFormat = PaperFormat,
        Orientation = Orientation,
        Copies = Copies,
        Duplex = DuplexEnabled ? DuplexMode.DuplexLongEdge : DuplexMode.Simplex
    };

    private void RefreshQueue()
    {
        var existing = Jobs.ToDictionary(j => j.Id);

        Jobs.Clear();
        foreach (var job in _queueService.Jobs)
        {
            if (existing.TryGetValue(job.Id, out var vm))
            {
                vm.SyncFrom(job);
                Jobs.Add(vm);
            }
            else
            {
                Jobs.Add(new PrintJobViewModel(job, _localization));
            }
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
            }
        }
        catch
        {
            // Offline-first: network errors must not block startup.
        }
    }
}
