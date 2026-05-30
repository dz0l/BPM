using System.Windows;

namespace PrintMaestro.Services;

public interface IHistoryDialogService
{
    void ShowHistory(Window owner);
}
