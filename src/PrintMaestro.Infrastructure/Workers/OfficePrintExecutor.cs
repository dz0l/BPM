using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using PrintMaestro.Core.IPC;
using PrintMaestro.Core.Models;
using PrintMaestro.Core.Threading;

namespace PrintMaestro.Infrastructure.Workers;

[SupportedOSPlatform("windows")]
public static class OfficePrintExecutor
{
    private const int WordAlertsNone = 0;
    private const int WordOrientPortrait = 0;
    private const int WordOrientLandscape = 1;
    private const int WordPaperA3 = 8;
    private const int WordPaperA4 = 9;

    private const int ExcelPagePortrait = 1;
    private const int ExcelPageLandscape = 2;
    private const int ExcelPaperA3 = 8;
    private const int ExcelPaperA4 = 9;

    private const int PowerPointVisibleTrue = -1;
    private const int PowerPointMsoTrue = -1;
    private const int PowerPointMsoFalse = 0;
    private const int PowerPointPrintInBackgroundFalse = 0;
    private const int PowerPointDisplayAlertsNone = 1;
    private const int PowerPointPrintAll = 1;
    private const int PowerPointSpoolerWaitMs = 5000;
    private const int PowerPointDefaultPrinterSettleMs = 300;

    public static WorkerResponse Execute(WorkerMessage message, CancellationToken cancellationToken)
    {
        OfficeProcessTracker.CleanupOrphanFromPreviousSession();

        var trackedPid = -1;
        using var cancelRegistration = cancellationToken.Register(() =>
        {
            if (trackedPid > 0)
            {
                OfficeProcessTracker.TryKillProcess(trackedPid);
            }
        });

        try
        {
            return StaThreadRunner.Run(
                () => ExecuteCore(message, pid => trackedPid = pid),
                CancellationToken.None,
                useBackgroundThread: false);
        }
        catch (OperationCanceledException)
        {
            return WorkerResponse.Fail("CANCELED", "Print canceled.", retryable: false);
        }
    }

    private static WorkerResponse ExecuteCore(WorkerMessage message, Action<int> trackProcessId)
    {
        try
        {
            if (!File.Exists(message.FilePath))
            {
                return WorkerResponse.Fail("FILE_NOT_FOUND", "File not found.", retryable: false);
            }

            var extension = Path.GetExtension(message.FilePath);

            return extension.ToLowerInvariant() switch
            {
                ".doc" or ".docx" => PrintWord(message, trackProcessId),
                ".xls" or ".xlsx" => PrintExcel(message, trackProcessId),
                ".ppt" or ".pptx" => PrintPowerPoint(message, trackProcessId),
                _ => WorkerResponse.Fail("UNSUPPORTED_OFFICE", "Unsupported Office format.", retryable: false)
            };
        }
        catch (COMException ex) when (IsPasswordProtected(ex))
        {
            return WorkerResponse.Fail(
                "PASSWORD_PROTECTED",
                "Document is password protected.",
                retryable: false);
        }
        catch (COMException ex)
        {
            return WorkerResponse.Fail("OFFICE_COM_ERROR", ex.Message, retryable: false);
        }
        catch (Exception ex)
        {
            return WorkerResponse.Fail("OFFICE_PRINT_FAILED", ex.Message);
        }
    }

    private static WorkerResponse PrintWord(WorkerMessage message, Action<int> trackProcessId)
    {
        var wordType = Type.GetTypeFromProgID("Word.Application");
        if (wordType is null)
        {
            return WorkerResponse.Fail(
                "OFFICE_NOT_INSTALLED",
                "Microsoft Word is not installed.",
                retryable: false);
        }

        dynamic? app = null;
        dynamic? document = null;

        try
        {
            app = Activator.CreateInstance(wordType)!;
            app.Visible = false;
            app.DisplayAlerts = WordAlertsNone;

            TrackOfficeProcess(app, trackProcessId);

            var printer = OfficePrinterResolver.TryResolveActivePrinter(app, message.Settings.PrinterName);
            if (printer is not null)
            {
                app.ActivePrinter = printer;
            }

            document = app.Documents.Open(
                message.FilePath,
                false,
                true,
                false);

            ApplyWordPageSetup(app, document, message.Settings);

            var copies = (short)Math.Clamp(message.Settings.Copies, 1, short.MaxValue);
            document.PrintOut(Background: false, Copies: copies);

            return WorkerResponse.Ok();
        }
        finally
        {
            if (document is not null)
            {
                try
                {
                    document.Close(SaveChanges: false);
                }
                catch
                {
                    // Ignore close errors after kill/cancel.
                }

                Marshal.ReleaseComObject(document);
            }

            if (app is not null)
            {
                try
                {
                    app.Quit(SaveChanges: false);
                }
                catch
                {
                    // Ignore quit errors after kill/cancel.
                }

                Marshal.ReleaseComObject(app);
            }

            OfficeProcessTracker.ClearTracking();
        }
    }

    private static WorkerResponse PrintExcel(WorkerMessage message, Action<int> trackProcessId)
    {
        var excelType = Type.GetTypeFromProgID("Excel.Application");
        if (excelType is null)
        {
            return WorkerResponse.Fail(
                "OFFICE_NOT_INSTALLED",
                "Microsoft Excel is not installed.",
                retryable: false);
        }

        dynamic? app = null;
        dynamic? workbook = null;

        try
        {
            app = Activator.CreateInstance(excelType)!;
            app.Visible = false;
            app.DisplayAlerts = false;

            TrackOfficeProcess(app, trackProcessId);

            var printer = OfficePrinterResolver.TryResolveActivePrinter(app, message.Settings.PrinterName);

            workbook = app.Workbooks.Open(message.FilePath, 0, true);

            ApplyExcelWorkbookPageSetup(workbook, message.Settings);

            var copies = (short)Math.Clamp(message.Settings.Copies, 1, short.MaxValue);
            if (printer is not null)
            {
                workbook.PrintOut(
                    Type.Missing,
                    Type.Missing,
                    copies,
                    false,
                    printer,
                    Type.Missing,
                    Type.Missing,
                    Type.Missing);
            }
            else
            {
                workbook.PrintOut(Copies: copies);
            }

            return WorkerResponse.Ok();
        }
        finally
        {
            if (workbook is not null)
            {
                try
                {
                    workbook.Close(SaveChanges: false);
                }
                catch
                {
                    // Ignore close errors after kill/cancel.
                }

                Marshal.ReleaseComObject(workbook);
            }

            if (app is not null)
            {
                try
                {
                    app.Quit();
                }
                catch
                {
                    // Ignore quit errors after kill/cancel.
                }

                Marshal.ReleaseComObject(app);
            }

            OfficeProcessTracker.ClearTracking();
        }
    }

    private static WorkerResponse PrintPowerPoint(WorkerMessage message, Action<int> trackProcessId)
    {
        var powerPointType = Type.GetTypeFromProgID("PowerPoint.Application");
        if (powerPointType is null)
        {
            return WorkerResponse.Fail(
                "OFFICE_NOT_INSTALLED",
                "Microsoft PowerPoint is not installed.",
                retryable: false);
        }

        var filePath = Path.GetFullPath(message.FilePath);
        var installedPrinter = OfficePrinterResolver.TryResolveInstalledPrinterName(message.Settings.PrinterName)
            ?? message.Settings.PrinterName;
        dynamic? app = null;
        dynamic? presentation = null;
        WindowsDefaultPrinterScope? defaultPrinterScope = null;

        try
        {
            defaultPrinterScope = WindowsDefaultPrinterScope.TryCreate(installedPrinter);
            if (!string.IsNullOrWhiteSpace(installedPrinter))
            {
                Thread.Sleep(PowerPointDefaultPrinterSettleMs);
            }

            app = Activator.CreateInstance(powerPointType)!;
            app.Visible = PowerPointVisibleTrue;
            TrySetPowerPointDisplayAlertsNone(app);

            TrackOfficeProcess(app, trackProcessId);

            presentation = app.Presentations.Open(
                filePath,
                PowerPointMsoTrue,
                PowerPointMsoTrue,
                PowerPointMsoFalse);

            dynamic printOptions = presentation.PrintOptions;
            printOptions.PrintInBackground = PowerPointPrintInBackgroundFalse;
            printOptions.RangeType = PowerPointPrintAll;

            if (!string.IsNullOrWhiteSpace(installedPrinter))
            {
                OfficePrinterResolver.TryApplyPowerPointPrintOptions(printOptions, installedPrinter);
            }

            var copies = (int)Math.Clamp(message.Settings.Copies, 1, short.MaxValue);
            if (copies > 1)
            {
                printOptions.NumberOfCopies = copies;
            }

            presentation.PrintOut();

            WaitForPowerPointSpooler();

            return WorkerResponse.Ok();
        }
        catch (COMException ex)
        {
            return WorkerResponse.Fail("OFFICE_PRINT_FAILED", ex.Message, retryable: false);
        }
        finally
        {
            if (presentation is not null)
            {
                try
                {
                    presentation.Close();
                }
                catch
                {
                    // Ignore close errors after kill/cancel.
                }

                Marshal.ReleaseComObject(presentation);
            }

            if (app is not null)
            {
                try
                {
                    app.Quit();
                }
                catch
                {
                    // Ignore quit errors after kill/cancel.
                }

                Marshal.ReleaseComObject(app);
            }

            defaultPrinterScope?.Dispose();
            OfficeProcessTracker.ClearTracking();
        }
    }

    private static void TrySetPowerPointDisplayAlertsNone(dynamic app)
    {
        try
        {
            app.DisplayAlerts = PowerPointDisplayAlertsNone;
        }
        catch
        {
            // Best effort: suppress printer error dialogs during automation.
        }
    }

    private static void WaitForPowerPointSpooler()
    {
        Thread.Sleep(PowerPointSpoolerWaitMs);
    }

    private static void TrackOfficeProcess(dynamic app, Action<int> trackProcessId)
    {
        try
        {
            nint hwnd = TryGetWindowHandle(app);
            var pid = OfficeProcessTracker.GetProcessIdFromWindowHandle(hwnd);
            if (pid > 0)
            {
                trackProcessId(pid);
                OfficeProcessTracker.TrackProcess(pid);
            }
        }
        catch
        {
            // Hwnd may be unavailable briefly after startup.
        }
    }

    private static nint TryGetWindowHandle(dynamic app)
    {
        try
        {
            return (int)app.Hwnd;
        }
        catch
        {
            // PowerPoint exposes HWND instead of Hwnd.
        }

        try
        {
            return (int)app.HWND;
        }
        catch
        {
            return 0;
        }
    }

    private static void ApplyWordPageSetup(dynamic app, dynamic document, PrintSettings settings)
    {
        dynamic sections = document.Sections;
        var count = (int)sections.Count;

        for (var index = 1; index <= count; index++)
        {
            dynamic section = sections[index];
            dynamic pageSetup = section.PageSetup;
            ApplyWordSectionPageSetup(app, pageSetup, settings);
            Marshal.ReleaseComObject(pageSetup);
            Marshal.ReleaseComObject(section);
        }

        Marshal.ReleaseComObject(sections);
    }

    private static void ApplyWordSectionPageSetup(dynamic app, dynamic pageSetup, PrintSettings settings)
    {
        var isLandscape = settings.Orientation == PaperOrientation.Landscape;

        if (settings.Orientation == PaperOrientation.Landscape)
        {
            pageSetup.Orientation = WordOrientLandscape;
        }
        else if (settings.Orientation == PaperOrientation.Portrait)
        {
            pageSetup.Orientation = WordOrientPortrait;
            isLandscape = false;
        }
        else if ((int)pageSetup.Orientation == WordOrientLandscape)
        {
            isLandscape = true;
        }

        if (settings.PaperFormat == PaperFormat.A3)
        {
            pageSetup.PaperSize = WordPaperA3;
            SetWordPageDimensions(app, pageSetup, isLandscape, 42.0, 29.7);
        }
        else
        {
            pageSetup.PaperSize = WordPaperA4;
            SetWordPageDimensions(app, pageSetup, isLandscape, 21.0, 29.7);
        }
    }

    private static void SetWordPageDimensions(
        dynamic app,
        dynamic pageSetup,
        bool landscape,
        double widthCm,
        double heightCm)
    {
        var width = landscape ? heightCm : widthCm;
        var height = landscape ? widthCm : heightCm;
        pageSetup.PageWidth = app.CentimetersToPoints(width);
        pageSetup.PageHeight = app.CentimetersToPoints(height);
    }

    private static void ApplyExcelWorkbookPageSetup(dynamic workbook, PrintSettings settings)
    {
        dynamic worksheets = workbook.Worksheets;
        var count = (int)worksheets.Count;

        for (var index = 1; index <= count; index++)
        {
            dynamic worksheet = worksheets[index];
            ApplyExcelWorksheetPageSetup(worksheet, settings);
            Marshal.ReleaseComObject(worksheet);
        }

        Marshal.ReleaseComObject(worksheets);
    }

    private static void ApplyExcelWorksheetPageSetup(dynamic worksheet, PrintSettings settings)
    {
        dynamic pageSetup = worksheet.PageSetup;

        if (settings.Orientation == PaperOrientation.Landscape)
        {
            pageSetup.Orientation = ExcelPageLandscape;
        }
        else if (settings.Orientation == PaperOrientation.Portrait)
        {
            pageSetup.Orientation = ExcelPagePortrait;
        }

        pageSetup.PaperSize = settings.PaperFormat == PaperFormat.A3
            ? ExcelPaperA3
            : ExcelPaperA4;

        Marshal.ReleaseComObject(pageSetup);
    }

    private static bool IsPasswordProtected(COMException exception) =>
        exception.Message.Contains("password", StringComparison.OrdinalIgnoreCase)
        || exception.Message.Contains("парол", StringComparison.OrdinalIgnoreCase);
}
