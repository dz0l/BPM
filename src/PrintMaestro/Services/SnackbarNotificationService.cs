using Wpf.Ui.Controls;

namespace PrintMaestro.Services;

public sealed class SnackbarNotificationService : INotificationService
{
    private SnackbarPresenter? _presenter;

    public void SetPresenter(SnackbarPresenter presenter) => _presenter = presenter;

    public void ShowSuccess(string message) => Show(message, ControlAppearance.Success);

    public void ShowWarning(string message) => Show(message, ControlAppearance.Caution);

    public void ShowError(string message) => Show(message, ControlAppearance.Danger);

    private void Show(string message, ControlAppearance appearance)
    {
        if (_presenter is null)
        {
            return;
        }

        void ShowInternal()
        {
            var snackbar = new Snackbar(_presenter)
            {
                Content = message,
                Appearance = appearance,
                Timeout = TimeSpan.FromSeconds(4)
            };

            snackbar.Show();
        }

        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            ShowInternal();
            return;
        }

        dispatcher.InvokeAsync(ShowInternal);
    }
}
