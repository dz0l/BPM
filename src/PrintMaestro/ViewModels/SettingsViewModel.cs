using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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

    public SettingsViewModel(ILocalizationService localization, ISettingsStore settingsStore)
    {
        _localization = localization;
        _settingsStore = settingsStore;
        AvailableCultures = _localization.SupportedCultures.ToList();
        SelectedCulture = _localization.CurrentCulture;
        AppVersion = AppVersionInfo.CurrentDisplay;
    }

    public IReadOnlyList<string> AvailableCultures { get; }

    public string AppVersion { get; }

    public ObservableCollection<PrintProfile> PrintProfiles { get; } = [];

    [ObservableProperty]
    private string _selectedCulture;

    [ObservableProperty]
    private PrintProfile? _selectedProfile;

    public event EventHandler? CloseRequested;

    public void LoadProfiles()
    {
        var settings = _settingsStore.Load();
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

    partial void OnSelectedCultureChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == _localization.CurrentCulture)
        {
            return;
        }

        _ = ApplyCultureAsync(value);
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
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);

    private async Task ApplyCultureAsync(string culture) =>
        await _localization.ApplyCultureAsync(culture);
}
