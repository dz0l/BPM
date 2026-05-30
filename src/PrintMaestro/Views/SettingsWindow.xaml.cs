using System.Windows;
using PrintMaestro.ViewModels;
using Wpf.Ui.Controls;

namespace PrintMaestro.Views;

public partial class SettingsWindow : FluentWindow
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
