using System.Windows;
using PrintMaestro.ViewModels;
using Wpf.Ui.Controls;

namespace PrintMaestro.Views;

public partial class MainWindow : FluentWindow
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += OnLoadedAsync;
    }

    private async void OnLoadedAsync(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.InitializeAsync(CancellationToken.None);
        }
    }
}
