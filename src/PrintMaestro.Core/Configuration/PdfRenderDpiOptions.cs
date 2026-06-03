namespace PrintMaestro.Core.Configuration;

public static class PdfRenderDpiOptions
{
    public const int Default = 200;

    public const int Min = 150;

    public const int Max = 400;

    public static int Clamp(int dpi) => Math.Clamp(dpi, Min, Max);
}
