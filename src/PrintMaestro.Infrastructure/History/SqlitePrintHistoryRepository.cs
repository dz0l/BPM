using Microsoft.Data.Sqlite;
using PrintMaestro.Core.Configuration;
using PrintMaestro.Core.History;
using PrintMaestro.Core.Models;

namespace PrintMaestro.Infrastructure.History;

public sealed class SqlitePrintHistoryRepository(IAppPaths appPaths) : IPrintHistoryRepository
{
    private const int MaxEntries = 100;

    private bool _initialized;

    public async Task AddAsync(PrintHistoryEntry entry, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO print_history
                (file_name, file_path, printer_name, user_name, start_time, end_time, success, error_message, copies)
            VALUES
                ($fileName, $filePath, $printerName, $userName, $startTime, $endTime, $success, $errorMessage, $copies);
            """;
        command.Parameters.AddWithValue("$fileName", entry.FileName);
        command.Parameters.AddWithValue("$filePath", entry.FilePath);
        command.Parameters.AddWithValue("$printerName", entry.PrinterName);
        command.Parameters.AddWithValue("$userName", entry.UserName);
        command.Parameters.AddWithValue("$startTime", entry.StartTime.UtcDateTime.ToString("O"));
        command.Parameters.AddWithValue("$endTime", entry.EndTime?.UtcDateTime.ToString("O") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$success", entry.Success ? 1 : 0);
        command.Parameters.AddWithValue("$errorMessage", entry.ErrorMessage ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$copies", entry.Copies);
        await command.ExecuteNonQueryAsync(cancellationToken);

        await TrimAsync(connection, cancellationToken);
    }

    public async Task<IReadOnlyList<PrintHistoryEntry>> GetRecentAsync(int limit, CancellationToken cancellationToken)
    {
        await EnsureInitializedAsync(cancellationToken);

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, file_name, file_path, printer_name, user_name, start_time, end_time, success, error_message, copies
            FROM print_history
            ORDER BY start_time DESC
            LIMIT $limit;
            """;
        command.Parameters.AddWithValue("$limit", limit);

        var entries = new List<PrintHistoryEntry>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            entries.Add(new PrintHistoryEntry
            {
                Id = reader.GetInt64(0),
                FileName = reader.GetString(1),
                FilePath = reader.GetString(2),
                PrinterName = reader.GetString(3),
                UserName = reader.GetString(4),
                StartTime = DateTimeOffset.Parse(reader.GetString(5)),
                EndTime = reader.IsDBNull(6) ? null : DateTimeOffset.Parse(reader.GetString(6)),
                Success = reader.GetInt32(7) == 1,
                ErrorMessage = reader.IsDBNull(8) ? null : reader.GetString(8),
                Copies = reader.GetInt32(9)
            });
        }

        return entries;
    }

    private async Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        if (_initialized)
        {
            return;
        }

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS print_history (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                file_name TEXT NOT NULL,
                file_path TEXT NOT NULL,
                printer_name TEXT NOT NULL,
                user_name TEXT NOT NULL,
                start_time TEXT NOT NULL,
                end_time TEXT NULL,
                success INTEGER NOT NULL,
                error_message TEXT NULL,
                copies INTEGER NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
        _initialized = true;
    }

    private static async Task TrimAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM print_history
            WHERE id NOT IN (
                SELECT id FROM print_history
                ORDER BY start_time DESC
                LIMIT $maxEntries
            );
            """;
        command.Parameters.AddWithValue("$maxEntries", MaxEntries);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private SqliteConnection CreateConnection() => new($"Data Source={appPaths.HistoryDatabasePath}");
}
