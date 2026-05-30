using PrintMaestro.Core.Models;
using PrintMaestro.Core.Printing;

namespace PrintMaestro.Core.Tests;

public class PrintLayoutCalculatorTests
{
    [Fact]
    public void ResolveOrientation_KeepsManualPortrait()
    {
        var resolved = PrintLayoutCalculator.ResolveOrientation(PaperOrientation.Portrait, 2000, 1000);

        Assert.Equal(PaperOrientation.Portrait, resolved);
    }

    [Fact]
    public void ResolveOrientation_KeepsManualLandscape()
    {
        var resolved = PrintLayoutCalculator.ResolveOrientation(PaperOrientation.Landscape, 100, 2000);

        Assert.Equal(PaperOrientation.Landscape, resolved);
    }

    [Fact]
    public void ResolveOrientation_AutoUsesLandscapeForWideContent()
    {
        var resolved = PrintLayoutCalculator.ResolveOrientation(PaperOrientation.Auto, 1200, 800);

        Assert.Equal(PaperOrientation.Landscape, resolved);
    }

    [Fact]
    public void ResolveOrientation_AutoUsesPortraitForTallContent()
    {
        var resolved = PrintLayoutCalculator.ResolveOrientation(PaperOrientation.Auto, 800, 1200);

        Assert.Equal(PaperOrientation.Portrait, resolved);
    }

    [Fact]
    public void ResolveOrientationFromPdfPage_UsesLandscapeForRotate90OnPortraitMediaBox()
    {
        var resolved = PrintLayoutCalculator.ResolveOrientationFromPdfPage(
            PaperOrientation.Auto,
            mediaWidth: 595,
            mediaHeight: 842,
            rotationDegrees: 90);

        Assert.Equal(PaperOrientation.Landscape, resolved);
    }

    [Fact]
    public void ResolveOrientationFromPdfPage_UsesLandscapeForWideMediaBox()
    {
        var resolved = PrintLayoutCalculator.ResolveOrientationFromPdfPage(
            PaperOrientation.Auto,
            mediaWidth: 842,
            mediaHeight: 595,
            rotationDegrees: 0);

        Assert.Equal(PaperOrientation.Landscape, resolved);
    }
}
