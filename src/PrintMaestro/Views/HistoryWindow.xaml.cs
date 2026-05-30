using PrintMaestro.ViewModels;

namespace PrintMaestro.Views;

public partial class HistoryWindow : Wpf.Ui.Controls.FluentWindow
{
    public HistoryWindow(HistoryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
