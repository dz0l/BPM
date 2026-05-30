using PrintMaestro.Core.Printing;

namespace PrintMaestro.Infrastructure.Printing;

public sealed class PrintDocumentHandlerRegistry
{
    private readonly IReadOnlyDictionary<DocumentKind, IPrintDocumentHandler> _handlers;

    public PrintDocumentHandlerRegistry(IEnumerable<IPrintDocumentHandler> handlers)
    {
        _handlers = handlers.ToDictionary(h => h.Kind);
    }

    public IPrintDocumentHandler? GetHandler(DocumentKind kind) =>
        _handlers.TryGetValue(kind, out var handler) ? handler : null;
}
