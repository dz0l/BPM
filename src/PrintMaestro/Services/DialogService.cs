using System.Windows;
using PrintMaestro.Views;

namespace PrintMaestro.Services;

public sealed class DialogService(ILocalizationService localization) : IDialogService
{
    public bool ConfirmShutdown()
    {
        var result = MessageBox.Show(
            localization.GetString("Dialog.Shutdown.Message"),
            localization.GetString("Dialog.Shutdown.Title"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        return result == MessageBoxResult.Yes;
    }

    public bool ConfirmInstallUpdate(string version)
    {
        var result = MessageBox.Show(
            localization.GetString("Dialog.Update.Message", version),
            localization.GetString("Dialog.Update.Title"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Information);

        return result == MessageBoxResult.Yes;
    }

    public void ShowAlreadyRunning()
    {
        MessageBox.Show(
            localization.GetString("Dialog.AlreadyRunning.Message"),
            localization.GetString("Dialog.AlreadyRunning.Title"),
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    public string? Prompt(Window owner, string title, string message, string? defaultText = null)
    {
        var window = new PromptWindow(title, message, defaultText)
        {
            Owner = owner
        };

        return window.ShowDialog() == true ? window.Result : null;
    }
}
