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
        StateChanged += OnWindowStateChanged;

        AllowDrop = true;
        AddHandler(DragDrop.PreviewDragOverEvent, new DragEventHandler(OnPreviewDragOver), true);
        AddHandler(DragDrop.PreviewDropEvent, new DragEventHandler(OnPreviewFileDrop), true);
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

        UpdateMaximizeButtonIcon();
    }

    private void OnWindowStateChanged(object? sender, EventArgs e) => UpdateMaximizeButtonIcon();

    private void UpdateMaximizeButtonIcon()
    {
        if (MaximizeWindowButton is null)
        {
            return;
        }

        MaximizeWindowButton.Icon = new SymbolIcon
        {
            Symbol = WindowState == WindowState.Maximized
                ? SymbolRegular.SquareMultiple24
                : SymbolRegular.Maximize24
        };

        MaximizeWindowButton.ToolTip = WindowState == WindowState.Maximized
            ? FindResource("Window.Restore")
            : FindResource("Window.Maximize");
    }

    private void OnMinimizeWindowClick(object sender, RoutedEventArgs e) =>
        WindowState = WindowState.Minimized;

    private void OnMaximizeWindowClick(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnCloseWindowClick(object sender, RoutedEventArgs e) => Close();

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

    private void OnPreviewDragOver(object sender, DragEventArgs e)
    {
        if (!IsExternalFileDrop(e))
        {
            return;
        }

        e.Effects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void OnPreviewFileDrop(object sender, DragEventArgs e)
    {
        if (!IsExternalFileDrop(e))
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

    private static bool IsExternalFileDrop(DragEventArgs e) =>
        e.Data.GetDataPresent(DataFormats.FileDrop)
        && e.Data.GetData(DataFormats.FileDrop) is string[];
}
