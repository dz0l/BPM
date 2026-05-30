using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PrintMaestro.ViewModels;
using PrintMaestro.Views;

namespace PrintMaestro.Services;

public sealed class HistoryDialogService(IServiceProvider serviceProvider) : IHistoryDialogService
{
    public void ShowHistory(Window owner)
    {
        var viewModel = ActivatorUtilities.CreateInstance<HistoryViewModel>(serviceProvider);
        var window = new HistoryWindow(viewModel)
        {
            Owner = owner
        };

        viewModel.CloseRequested += (_, _) => window.Close();
        _ = viewModel.LoadAsync();
        window.ShowDialog();
    }
}
