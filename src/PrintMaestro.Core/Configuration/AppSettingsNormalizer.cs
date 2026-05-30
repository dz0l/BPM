namespace PrintMaestro.Core.Configuration;

public static class AppSettingsNormalizer
{
    public static AppSettings Normalize(AppSettings settings)
    {
        settings.DefaultPrintSettings ??= new Models.PrintSettings();
        settings.PrintProfiles ??= [];

        foreach (var profile in settings.PrintProfiles)
        {
            profile.Settings ??= new Models.PrintSettings();
        }

        return settings;
    }
}
