using PrintMaestro.Core.Models;

namespace PrintMaestro.ViewModels;

public sealed class PrintProfileItemViewModel
{
    public PrintProfileItemViewModel(PrintProfile profile)
    {
        Name = profile.Name;
        Settings = profile.Settings.Clone();
    }

    public string Name { get; }

    public PrintSettings Settings { get; }
}
