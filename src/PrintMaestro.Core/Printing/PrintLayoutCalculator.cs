using PrintMaestro.Core.Models;

namespace PrintMaestro.Core.Printing;

public static class PrintLayoutCalculator
{
    public static PaperOrientation ResolveOrientation(
        PaperOrientation orientation,
        double contentWidth,
        double contentHeight)
    {
        if (orientation is PaperOrientation.Portrait or PaperOrientation.Landscape)
        {
            return orientation;
        }

        if (contentWidth <= 0 || contentHeight <= 0)
        {
            return PaperOrientation.Portrait;
        }

        return contentWidth >= contentHeight
            ? PaperOrientation.Landscape
            : PaperOrientation.Portrait;
    }

    public static PaperOrientation ResolveOrientationFromPdfPage(
        PaperOrientation orientation,
        double mediaWidth,
        double mediaHeight,
        int rotationDegrees)
    {
        if (orientation is PaperOrientation.Portrait or PaperOrientation.Landscape)
        {
            return orientation;
        }

        var effectiveWidth = rotationDegrees is 90 or 270 ? mediaHeight : mediaWidth;
        var effectiveHeight = rotationDegrees is 90 or 270 ? mediaWidth : mediaHeight;

        return ResolveOrientation(PaperOrientation.Auto, effectiveWidth, effectiveHeight);
    }
}
