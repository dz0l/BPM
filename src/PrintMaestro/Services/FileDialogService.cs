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
        var folder = PickFolder();
        if (string.IsNullOrWhiteSpace(folder))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
            .Where(SupportedFileTypes.IsSupported)
            .ToList();
    }

    public string? PickFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Multiselect = false
        };

        return dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FolderName)
            ? dialog.FolderName
            : null;
    }
}
