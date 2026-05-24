using Dapper;
using Npgsql;

namespace TicketPlatform.Data.Repositories;

public record EventListItem(
    int Id,
    string Title,
    string Venue,
    DateTime EventDate,
    decimal? MinPrice,
    int AvailableTickets);

public interface IEventSearchRepository
{
    Task<IReadOnlyList<EventListItem>> SearchAsync(string? query, CancellationToken ct);
}

// Dapper-based read repository (Data Mapper). Uses parameterized SQL — protected from SQL injection.
public class EventSearchRepository : IEventSearchRepository
{
    private readonly string _connectionString;

    public EventSearchRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<EventListItem>> SearchAsync(string? query, CancellationToken ct)
    {
        const string sql = @"
            SELECT  e.""Id""           AS Id,
                    e.""Title""        AS Title,
                    e.""Venue""        AS Venue,
                    e.""EventDate""    AS EventDate,
                    MIN(tc.""BasePrice"") AS MinPrice,
                    COALESCE(SUM(tc.""TotalQuantity"" - tc.""SoldQuantity""), 0)::int AS AvailableTickets
            FROM    ""Events"" e
            LEFT JOIN ""TicketCategories"" tc ON tc.""EventId"" = e.""Id""
            WHERE   (@q IS NULL OR e.""Title"" ILIKE '%' || @q || '%' OR e.""Venue"" ILIKE '%' || @q || '%')
            GROUP BY e.""Id"", e.""Title"", e.""Venue"", e.""EventDate""
            ORDER BY e.""EventDate"" ASC;";

        await using var conn = new NpgsqlConnection(_connectionString);
        var rows = await conn.QueryAsync<EventListItem>(
            new CommandDefinition(sql, new { q = query }, cancellationToken: ct));
        return rows.AsList();
    }
}
