using TicketPlatform.Data.Entities;

namespace TicketPlatform.Business.Services;

public record EventInputCategory(int? Id, string Name, decimal BasePrice, int TotalQuantity, uint? Xmin);
public record EventInput(string Title, string Venue, string Description, DateTime EventDate,
    PricingStrategyType PricingStrategy, IReadOnlyList<EventInputCategory> Categories);

public interface IEventService
{
    Task<Event?> GetAsync(int id, CancellationToken ct);
    Task<Event> CreateAsync(EventInput input, CancellationToken ct);

    // expectedXmin is the version the client last fetched. If DB has moved on, throws DbUpdateConcurrencyException.
    Task<Event> UpdateAsync(int id, uint expectedXmin, EventInput input, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}
