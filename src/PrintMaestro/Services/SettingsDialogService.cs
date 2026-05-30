using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PrintMaestro.ViewModels;
using PrintMaestro.Views;

namespace PrintMaestro.Services;

public sealed class SettingsDialogService(IServiceProvider serviceProvider) : ISettingsDialogService
{
    public void ShowSettings(Window owner)
    {
        try
        {
            var viewModel = ActivatorUtilities.CreateInstance<SettingsViewModel>(serviceProvider);
            viewModel.LoadProfiles();

            var window = new SettingsWindow(viewModel)
            {
                Owner = owner
            };

            viewModel.CloseRequested += (_, _) => window.Close();
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to open settings window.");
            System.Windows.MessageBox.Show(
                ex.Message,
                owner.Title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
