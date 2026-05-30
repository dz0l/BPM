namespace PrintMaestro.Core.Models;

public sealed class PrintProfile
{
    public string Name { get; set; } = string.Empty;

    public PrintSettings Settings { get; set; } = new();
}
