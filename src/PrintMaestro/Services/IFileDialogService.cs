namespace PrintMaestro.Services;

public interface IFileDialogService
{
    IReadOnlyList<string> OpenDocuments();

    IReadOnlyList<string> OpenFolder();
}
