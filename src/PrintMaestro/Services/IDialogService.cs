using System.Windows;

namespace PrintMaestro.Services;

public interface IDialogService
{
    bool ConfirmShutdown();

    bool ConfirmInstallUpdate(string version);

    void ShowAlreadyRunning();

    string? Prompt(Window owner, string title, string message, string? defaultText = null);
}
