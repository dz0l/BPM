using Microsoft.Extensions.DependencyInjection;
using PrintMaestro.Core.Configuration;
using PrintMaestro.Core.History;
using PrintMaestro.Core.Printing;
using PrintMaestro.Core.Printers;
using PrintMaestro.Core.Updates;
using PrintMaestro.Infrastructure.Configuration;
using PrintMaestro.Infrastructure.History;
using PrintMaestro.Infrastructure.Printers;
using PrintMaestro.Infrastructure.Printing.Handlers;
using PrintMaestro.Infrastructure.Updates;
using Serilog;

namespace PrintMaestro.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPrintMaestroInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IAppPaths, AppPaths>();
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();
        services.AddSingleton<IPrintHistoryRepository, SqlitePrintHistoryRepository>();
        services.AddSingleton<IPrintQueueService, PrintQueueService>();
        services.AddSingleton<IPrintDispatcher, NoOpPrintDispatcher>();
        services.AddSingleton<IPrinterDiscoveryService, SystemPrinterDiscoveryService>();

        services.AddSingleton<IPrintDocumentHandler, PdfPrintDocumentHandler>();
        services.AddSingleton<IPrintDocumentHandler, OfficePrintDocumentHandler>();
        services.AddSingleton<IPrintDocumentHandler, ImagePrintDocumentHandler>();
        services.AddSingleton<IPrintDocumentHandler, TextPrintDocumentHandler>();

        services.AddHttpClient<IUpdateService, GitHubUpdateService>(client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("PrintMaestro-Updater/0.1");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }

    public static Serilog.ILogger CreateSerilogLogger(IAppPaths appPaths)
    {
        return new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                path: Path.Combine(appPaths.LogDirectory, "print-maestro-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 5,
                fileSizeLimitBytes: 10 * 1024 * 1024,
                rollOnFileSizeLimit: true)
            .CreateLogger();
    }
}
