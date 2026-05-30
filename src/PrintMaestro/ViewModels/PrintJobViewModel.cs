using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using PrintMaestro.Core.Models;
using PrintMaestro.Services;

namespace PrintMaestro.ViewModels;

public sealed partial class PrintJobViewModel : ObservableObject
{
    private readonly ILocalizationService _localization;
    private readonly IThumbnailService _thumbnailService;
    private PrintJobStatus _status;

    public PrintJobViewModel(PrintJob job, ILocalizationService localization, IThumbnailService thumbnailService)
    {
        _localization = localization;
        _thumbnailService = thumbnailService;
        Id = job.Id;
        FileName = job.FileName;
        FilePath = job.FilePath;
        _status = job.Status;
        ProgressPercent = job.ProgressPercent;
        ErrorMessage = job.ErrorMessage;

        _localization.CultureChanged += OnCultureChanged;
        _ = LoadThumbnailAsync();
    }

    public Guid Id { get; }

    public string FileName { get; }

    public string FilePath { get; }

    public PrintJobStatus Status
    {
        get => _status;
        private set
        {
            if (SetProperty(ref _status, value))
            {
                OnPropertyChanged(nameof(LocalizedStatus));
            }
        }
    }

    public string LocalizedStatus => _localization.GetString($"JobStatus.{Status}");

    [ObservableProperty]
    private int _progressPercent;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private ImageSource? _thumbnail;

    public void SyncFrom(PrintJob job)
    {
        Status = job.Status;
        ProgressPercent = job.ProgressPercent;
        ErrorMessage = job.ErrorMessage;
    }

    private void OnCultureChanged(object? sender, EventArgs e) =>
        OnPropertyChanged(nameof(LocalizedStatus));

    private async Task LoadThumbnailAsync()
    {
        try
        {
            Thumbnail = await _thumbnailService.GetThumbnailAsync(FilePath);
        }
        catch
        {
            // Thumbnails are optional UI enrichment.
        }
    }
}
