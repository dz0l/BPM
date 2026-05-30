using PrintMaestro.Core.Printing;

namespace PrintMaestro.Core.Printing;

/// <summary>
/// Абстракция обработчика печати для конкретного типа документа.
/// Реализации (PDFiumSharp, SkiaSharp, COM) подменяются без изменения диспетчера.
/// </summary>
public interface IPrintDocumentHandler
{
    DocumentKind Kind { get; }

    Task<PrintResult> PrintAsync(PrintRequest request, CancellationToken cancellationToken);
}
