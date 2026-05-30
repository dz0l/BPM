using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PrintMaestro.Infrastructure.DependencyInjection;
using PrintMaestro.Services;
using PrintMaestro.ViewModels;
using PrintMaestro.Views;
using Serilog;

namespace PrintMaestro;

public partial class App : Application
{
    public static ResourceDictionary LocalizationResources { get; } = new();

    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (!SingleInstanceGuard.TryAcquire())
        {
            MessageBox.Show(
                "Print Maestro is already running.",
                "Print Maestro",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Shutdown();
            return;
        }

        Resources.MergedDictionaries.Add(LocalizationResources);

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices(services =>
            {
                services.AddPrintMaestroInfrastructure();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IFileDialogService, FileDialogService>();
                services.AddSingleton<ILocalizationService, JsonLocalizationService>();
                services.AddSingleton<ISettingsDialogService, SettingsDialogService>();
                services.AddSingleton<IHistoryDialogService, HistoryDialogService>();
                services.AddSingleton<INotificationService, SnackbarNotificationService>();
                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<IThumbnailService, ThumbnailService>();
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        var appPaths = _host.Services.GetRequiredService<PrintMaestro.Core.Configuration.IAppPaths>();
        Log.Logger = ServiceCollectionExtensions.CreateSerilogLogger(appPaths);

        await _host.StartAsync();

        var localization = _host.Services.GetRequiredService<ILocalizationService>();
        if (localization is JsonLocalizationService jsonLocalization)
        {
            jsonLocalization.SetTargetDictionary(LocalizationResources);
        }

        await localization.InitializeAsync();

        DispatcherUnhandledException += (_, args) =>
        {
            Log.Error(args.Exception, "Unhandled UI exception.");
            MessageBox.Show(
                args.Exception.Message,
                "Print Maestro",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        SingleInstanceGuard.Release();

        if (_host is not null)
        {
            if (_host.Services.GetService(typeof(PrintMaestro.Infrastructure.Workers.IWorkerPrintService))
                is IAsyncDisposable workerPrintService)
            {
                await workerPrintService.DisposeAsync();
            }

            await _host.StopAsync();
            _host.Dispose();
        }

        await Log.CloseAndFlushAsync();
        base.OnExit(e);
    }
}
