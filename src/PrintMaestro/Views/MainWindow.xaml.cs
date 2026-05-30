using System.ComponentModel;
using System.Windows;
using PrintMaestro.Services;
using PrintMaestro.ViewModels;
using Wpf.Ui.Controls;

namespace PrintMaestro.Views;

public partial class MainWindow : FluentWindow
{
    private readonly IDialogService _dialogService;
    private readonly INotificationService _notificationService;

    public MainWindow(
        MainWindowViewModel viewModel,
        IDialogService dialogService,
        INotificationService notificationService)
    {
        _dialogService = dialogService;
        _notificationService = notificationService;

        InitializeComponent();
        DataContext = viewModel;

        Loaded += OnLoadedAsync;
        Closing += OnClosing;
        DragOver += OnDragOver;
        Drop += OnDrop;
    }

    private async void OnLoadedAsync(object sender, RoutedEventArgs e)
    {
        if (_notificationService is SnackbarNotificationService snackbarService)
        {
            snackbarService.SetPresenter(SnackbarPresenter);
        }

        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.InitializeAsync(CancellationToken.None);
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        if (viewModel.ShouldConfirmShutdown() && !_dialogService.ConfirmShutdown())
        {
            e.Cancel = true;
            return;
        }

        viewModel.StopPrintingAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
            return;
        }

        e.Effects = DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        if (e.Data.GetData(DataFormats.FileDrop) is not string[] paths || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        viewModel.AddDroppedPaths(paths);
        e.Handled = true;
    }
}
