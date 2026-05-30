namespace PrintMaestro.Core.Models;

public sealed class PrintSettings
{
    public string PrinterName { get; set; } = string.Empty;

    public PaperFormat PaperFormat { get; set; } = PaperFormat.A4;

    public PaperOrientation Orientation { get; set; } = PaperOrientation.Portrait;

    public int Copies { get; set; } = 1;

    public DuplexMode Duplex { get; set; } = DuplexMode.Simplex;

    public PrintSettings Clone() => new()
    {
        PrinterName = PrinterName,
        PaperFormat = PaperFormat,
        Orientation = Orientation,
        Copies = Copies,
        Duplex = Duplex
    };
}
