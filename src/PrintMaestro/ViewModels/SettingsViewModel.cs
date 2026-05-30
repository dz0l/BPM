using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrintMaestro.Services;

namespace PrintMaestro.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ILocalizationService _localization;

    public SettingsViewModel(ILocalizationService localization)
    {
        _localization = localization;
        AvailableCultures = _localization.SupportedCultures.ToList();
        SelectedCulture = _localization.CurrentCulture;
    }

    public IReadOnlyList<string> AvailableCultures { get; }

    [ObservableProperty]
    private string _selectedCulture;

    public event EventHandler? CloseRequested;

    partial void OnSelectedCultureChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value == _localization.CurrentCulture)
        {
            return;
        }

        _ = ApplyCultureAsync(value);
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(this, EventArgs.Empty);

    private async Task ApplyCultureAsync(string culture)
    {
        await _localization.ApplyCultureAsync(culture);
    }
}
