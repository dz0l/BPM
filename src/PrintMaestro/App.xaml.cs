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

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        await Log.CloseAndFlushAsync();
        base.OnExit(e);
    }
}
