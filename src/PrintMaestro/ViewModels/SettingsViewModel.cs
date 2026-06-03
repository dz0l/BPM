using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrintMaestro.Core;
using PrintMaestro.Core.Configuration;
using PrintMaestro.Core.Models;
using PrintMaestro.Services;

namespace PrintMaestro.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ILocalizationService _localization;
    private readonly ISettingsStore _settingsStore;
    private readonly ICurrentPrintSettingsSource _printSettingsSource;
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly IDialogService _dialogService;
    private readonly INotificationService _notificationService;
    private bool _isLoadingSettings;

    public SettingsViewModel(
        ILocalizationService localization,
        ISettingsStore settingsStore,
        ICurrentPrintSettingsSource printSettingsSource,
        MainWindowViewModel mainWindowViewModel,
        IDialogService dialogService,
        INotificationService notificationService)
    {
        _localization = localization;
        _settingsStore = settingsStore;
        _printSettingsSource = printSettingsSource;
        _mainWindowViewModel = mainWindowViewModel;
        _dialogService = dialogService;
        _notificationService = notificationService;
        AvailableCultures = _localization.SupportedCultures.ToList();
        SelectedCulture = _localization.CurrentCulture;
        AppVersion = AppVersionInfo.CurrentDisplay;
        PdfRenderDpi = PdfRenderDpiOptions.Default;
    }

    public IReadOnlyList<string> AvailableCultures { get; }

    public string AppVersion { get; }

    public ObservableCollection<PrintProfile> PrintProfiles { get; } = [];

    [ObservableProperty]
    private string _selectedCulture;

    [ObservableProperty]
    private PrintProfile? _selectedProfile;

    [ObservableProperty]
    private int _pdfRenderDpi = PdfRenderDpiOptions.Default;

    public event EventHandler? CloseRequested;

    public void LoadProfiles()
    {
        var settings = _settingsStore.Load();
        _isLoadingSettings = true;
        try
        {
            PdfRenderDpi = settings.PdfRenderDpi;
        }
        finally
        {
            _isLoadingSettings = false;
        }

        PrintProfiles.Clear();

        foreach (var profile in settings.PrintProfiles.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase))
        {
            PrintProfiles.Add(new PrintProfile
            {
                Name = profile.Name,
                Settings = profile.Settings.Clone()
            });
        }
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        LoadProfiles();
        return Task.CompletedTask;
    }

    partial void OnPdfRenderDpiChanged(int value)
    {
        if (_isLoadingSettings)
        {
            return;
        }

        var clamped = PdfRenderDpiOptions.Clamp(value);
        if (clamped != value)
        {
            PdfRenderDpi = clamped;
            return;
        }

        _ = PersistPdfRenderDpiAsync();
    }

    partial void OnSelectedCultureChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == _localization.CurrentCulture)
        {
            return;
        }

        _ = ApplyCultureAsync(value);
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        if (Application.Current.MainWindow is not Window owner)
        {
            return;
        }

        var name = _dialogService.Prompt(
            owner,
            _localization.GetString("Profiles.SaveTitle"),
            _localization.GetString("Profiles.SavePrompt"));

        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var trimmed = name.Trim();
        var settings = await _settingsStore.LoadAsync(CancellationToken.None);
        settings.PrintProfiles.RemoveAll(p => p.Name.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
        settings.PrintProfiles.Add(new PrintProfile
        {
            Name = trimmed,
            Settings = _printSettingsSource.CaptureCurrentPrintSettings().Clone()
        });
        await _settingsStore.SaveAsync(settings, CancellationToken.None);
        LoadProfiles();
        await _mainWindowViewModel.RefreshPrintProfilesAsync();

        _mainWindowViewModel.SelectedPrintProfile = _mainWindowViewModel.PrintProfiles
            .FirstOrDefault(p => p.Name.Equals(trimmed, StringComparison.OrdinalIgnoreCase));

        _notificationService.ShowSuccess(_localization.GetString("Profiles.Saved", trimmed));
    }

    [RelayCommand]
    private async Task DeleteProfileAsync()
    {
        if (SelectedProfile is null)
        {
            return;
        }

        var settings = await _settingsStore.LoadAsync(CancellationToken.None);
        settings.PrintProfiles.RemoveAll(p =>
            p.Name.Equals(SelectedProfile.Name, StringComparison.OrdinalIgnoreCase));
        await _settingsStore.SaveAsync(settings, CancellationToken.None);

        PrintProfiles.Remove(SelectedProfile);
        SelectedProfile = null;
        await _mainWindowViewModel.RefreshPrintProfilesAsync();
    }

    [RelayCommand]
    private void OpenThirdPartyLicenses()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "THIRD_PARTY_LICENSES.txt");
        if (!File.Exists(path))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    [RelayCommand]
    private void Close()
    {
        _ = PersistPdfRenderDpiAsync();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private async Task PersistPdfRenderDpiAsync()
    {
        var settings = await _settingsStore.LoadAsync(CancellationToken.None);
        settings.PdfRenderDpi = PdfRenderDpiOptions.Clamp(PdfRenderDpi);
        await _settingsStore.SaveAsync(settings, CancellationToken.None);
    }

    private async Task ApplyCultureAsync(string culture) =>
        await _localization.ApplyCultureAsync(culture);
}
