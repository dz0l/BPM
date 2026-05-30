using System.Globalization;

namespace PrintMaestro.Core.Localization;

public static class LocaleDetector
{
    public const string DefaultCulture = "en";

    public static string DetectSupportedCulture(IReadOnlyCollection<string> supportedCultures)
    {
        var uiCulture = CultureInfo.CurrentUICulture;
        var twoLetter = uiCulture.TwoLetterISOLanguageName.ToLowerInvariant();

        if (supportedCultures.Contains(twoLetter))
        {
            return twoLetter;
        }

        var parent = uiCulture.Parent.TwoLetterISOLanguageName.ToLowerInvariant();
        if (supportedCultures.Contains(parent))
        {
            return parent;
        }

        return DefaultCulture;
    }
}
