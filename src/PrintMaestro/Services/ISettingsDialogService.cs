using System.Windows;

namespace PrintMaestro.Services;

public interface ISettingsDialogService
{
    void ShowSettings(Window owner);
}
