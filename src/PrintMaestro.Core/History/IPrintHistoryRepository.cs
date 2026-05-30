using PrintMaestro.Core.Models;

namespace PrintMaestro.Core.History;

public interface IPrintHistoryRepository
{
    Task AddAsync(PrintHistoryEntry entry, CancellationToken cancellationToken);

    Task<IReadOnlyList<PrintHistoryEntry>> GetRecentAsync(int limit, CancellationToken cancellationToken);
}
