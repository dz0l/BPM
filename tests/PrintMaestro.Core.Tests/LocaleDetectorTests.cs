using PrintMaestro.Core.Localization;

namespace PrintMaestro.Core.Tests;

public class LocaleDetectorTests
{
    [Theory]
    [InlineData("en-US", "en")]
    [InlineData("ru-RU", "ru")]
    [InlineData("de-DE", "en")]
    public void DetectSupportedCulture_MapsKnownLocales(string uiCulture, string expected)
    {
        var original = Thread.CurrentThread.CurrentUICulture;
        try
        {
            Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(uiCulture);
            var result = LocaleDetector.DetectSupportedCulture(["en", "ru"]);
            Assert.Equal(expected, result);
        }
        finally
        {
            Thread.CurrentThread.CurrentUICulture = original;
        }
    }
}
