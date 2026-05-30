using PrintMaestro.Core.Models;

namespace PrintMaestro.Core.Printing;

public sealed class PrintRequest
{
    public required Guid JobId { get; init; }

    public required string FilePath { get; init; }

    public required PrintSettings Settings { get; init; }

    public required DocumentKind DocumentKind { get; init; }
}
