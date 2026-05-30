using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PrintMaestro.Core.Configuration;

namespace PrintMaestro.Services;

public sealed class ShellThumbnailProvider
{
    private const int ThumbnailSize = 256;

    [Flags]
    private enum SIIGBF
    {
        ResizeToFit = 0x00,
        BiggerSizeOk = 0x01,
        MemoryOnly = 0x02,
        IconOnly = 0x04,
        ThumbnailOnly = 0x08,
        InCacheOnly = 0x10
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SIZE
    {
        public int Width;
        public int Height;
    }

    [ComImport]
    [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem
    {
    }

    [ComImport]
    [Guid("bfbe0e2f-89ed-456d-8f69-3e051c45c7d3")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemImageFactory
    {
        void GetImage(SIZE size, SIIGBF flags, out IntPtr phbm);
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int SHCreateItemFromParsingName(
        [MarshalAs(UnmanagedType.LPWStr)] string path,
        IntPtr pbc,
        ref Guid riid,
        out IShellItem ppv);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    public ImageSource? GetThumbnail(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return null;
        }

        var shellItemGuid = typeof(IShellItem).GUID;
        var hr = SHCreateItemFromParsingName(filePath, IntPtr.Zero, ref shellItemGuid, out var shellItem);
        if (hr != 0 || shellItem is null)
        {
            return null;
        }

        var factory = (IShellItemImageFactory)shellItem;
        var size = new SIZE { Width = ThumbnailSize, Height = ThumbnailSize };
        factory.GetImage(size, SIIGBF.ResizeToFit | SIIGBF.ThumbnailOnly, out var hBitmap);

        if (hBitmap == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            using var bitmap = Image.FromHbitmap(hBitmap);
            return ConvertToImageSource(bitmap);
        }
        finally
        {
            DeleteObject(hBitmap);
        }
    }

    private static ImageSource ConvertToImageSource(Bitmap bitmap)
    {
        var handle = bitmap.GetHbitmap();
        try
        {
            return Imaging.CreateBitmapSourceFromHBitmap(
                handle,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
        }
        finally
        {
            DeleteObject(handle);
        }
    }
}
