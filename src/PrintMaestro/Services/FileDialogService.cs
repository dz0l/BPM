using System.IO;
using Microsoft.Win32;
using PrintMaestro.Core.Models;

namespace PrintMaestro.Services;

public sealed class FileDialogService : IFileDialogService
{
    public IReadOnlyList<string> OpenDocuments()
    {
        var dialog = new OpenFileDialog
        {
            Filter = SupportedFileTypes.FilterDescription,
            Multiselect = true,
            CheckFileExists = true
        };

        return dialog.ShowDialog() == true
            ? dialog.FileNames.ToList()
            : [];
    }

    public IReadOnlyList<string> OpenFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Multiselect = false
        };

        if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.FolderName))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(dialog.FolderName, "*.*", SearchOption.AllDirectories)
            .Where(SupportedFileTypes.IsSupported)
            .ToList();
    }
}
