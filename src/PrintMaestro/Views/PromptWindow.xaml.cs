using System.Windows;

namespace PrintMaestro.Views;

public partial class PromptWindow : Wpf.Ui.Controls.FluentWindow
{
    public PromptWindow(string title, string prompt, string? defaultText = null)
    {
        InitializeComponent();
        Title = title;
        PromptText.Text = prompt;
        InputTextBox.Text = defaultText ?? string.Empty;
    }

    public string? Result { get; private set; }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        Result = InputTextBox.Text;
        DialogResult = true;
        Close();
    }
}
