using PrintMaestro.Core.Models;

namespace PrintMaestro.Services;

public interface ICurrentPrintSettingsSource
{
    PrintSettings CaptureCurrentPrintSettings();
}
