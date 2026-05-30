using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PrintMaestro.ViewModels;
using PrintMaestro.Views;

namespace PrintMaestro.Services;

public sealed class SettingsDialogService(IServiceProvider serviceProvider) : ISettingsDialogService
{
    public void ShowSettings(Window owner)
    {
        var viewModel = ActivatorUtilities.CreateInstance<SettingsViewModel>(serviceProvider);
        var window = new SettingsWindow(viewModel)
        {
            Owner = owner
        };

        viewModel.CloseRequested += (_, _) => window.Close();
        window.ShowDialog();
    }
}
