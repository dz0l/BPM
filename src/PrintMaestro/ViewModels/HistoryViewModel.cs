using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrintMaestro.Core.History;
using PrintMaestro.Services;

namespace PrintMaestro.ViewModels;

public sealed partial class HistoryViewModel : ObservableObject
{
    private const int HistoryLimit = 100;

    private readonly IPrintHistoryRepository _historyRepository;
    private readonly ILocalizationService _localization;

    public HistoryViewModel(IPrintHistoryRepository historyRepository, ILocalizationService localization)
    {
        _historyRepository = historyRepository;
        _localization = localization;
    }

    public ObservableCollection<PrintHistoryEntryViewModel> Entries { get; } = [];

    public event EventHandler? CloseRequested;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var entries = await _historyRepository.GetRecentAsync(HistoryLimit, cancellationToken);
        Entries.Clear();

        foreach (var entry in entries)
        {
            Entries.Add(new PrintHistoryEntryViewModel
            {
                FileName = entry.FileName,
                PrinterName = entry.PrinterName,
                StartedAtDisplay = entry.StartTime.ToLocalTime().ToString("g"),
                ResultDisplay = entry.Success
                    ? _localization.GetString("History.Result.Success")
                    : _localization.GetString("History.Result.Failed"),
                ErrorMessage = entry.ErrorMessage,
                Copies = entry.Copies
            });
        }
    }

    [RelayCommand]
    private async Task ClearHistoryAsync()
    {
        await _historyRepository.ClearAsync(CancellationToken.None);
        Entries.Clear();
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);
}
