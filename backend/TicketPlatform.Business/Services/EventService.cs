using Microsoft.EntityFrameworkCore;
using TicketPlatform.Data;
using TicketPlatform.Data.Entities;

namespace TicketPlatform.Business.Services;

public class EventService : IEventService
{
    private readonly AppDbContext _db;

    public EventService(AppDbContext db) { _db = db; }

    public Task<Event?> GetAsync(int id, CancellationToken ct) =>
        // AsNoTracking ensures we read the actual DB state even if this DbContext has stale tracked entities
        // (e.g. after a concurrency-conflict rollback).
        _db.Events.AsNoTracking().Include(e => e.Categories).SingleOrDefaultAsync(e => e.Id == id, ct);

    public async Task<Event> CreateAsync(EventInput input, CancellationToken ct)
    {
        var ev = new Event
        {
            Title = input.Title,
            Venue = input.Venue,
            Description = input.Description,
            EventDate = DateTime.SpecifyKind(input.EventDate, DateTimeKind.Utc),
            PricingStrategy = input.PricingStrategy,
            Categories = input.Categories.Select(c => new TicketCategory
            {
                Name = c.Name,
                BasePrice = c.BasePrice,
                TotalQuantity = c.TotalQuantity
            }).ToList()
        };
        _db.Events.Add(ev);
        await _db.SaveChangesAsync(ct);
        return ev;
    }

    public async Task<Event> UpdateAsync(int id, uint expectedXmin, EventInput input, CancellationToken ct)
    {
        var ev = await _db.Events.Include(e => e.Categories).SingleOrDefaultAsync(e => e.Id == id, ct)
            ?? throw new KeyNotFoundException($"Event {id} not found.");

        ev.Title = input.Title;
        ev.Venue = input.Venue;
        ev.Description = input.Description;
        ev.EventDate = DateTime.SpecifyKind(input.EventDate, DateTimeKind.Utc);
        ev.PricingStrategy = input.PricingStrategy;

        // Override OriginalValue with the value the client last saw, so EF's
        // UPDATE ... WHERE xmin = @expected fails if another tab/admin moved the row.
        _db.Entry(ev).Property(e => e.Xmin).OriginalValue = expectedXmin;

        // Category sync (simple: replace set; preserves SoldQuantity by matching Id).
        var incomingIds = input.Categories.Where(c => c.Id.HasValue).Select(c => c.Id!.Value).ToHashSet();
        foreach (var existing in ev.Categories.ToList())
            if (!incomingIds.Contains(existing.Id))
                _db.TicketCategories.Remove(existing);

        foreach (var cat in input.Categories)
        {
            if (cat.Id is int existingId)
            {
                var match = ev.Categories.SingleOrDefault(c => c.Id == existingId);
                if (match is null) continue;
                match.Name = cat.Name;
                match.BasePrice = cat.BasePrice;
                match.TotalQuantity = cat.TotalQuantity;
                if (cat.Xmin.HasValue)
                    _db.Entry(match).Property(c => c.Xmin).OriginalValue = cat.Xmin.Value;
            }
            else
            {
                ev.Categories.Add(new TicketCategory
                {
                    Name = cat.Name,
                    BasePrice = cat.BasePrice,
                    TotalQuantity = cat.TotalQuantity
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        return ev;
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var ev = await _db.Events.SingleOrDefaultAsync(e => e.Id == id, ct);
        if (ev is null) return;
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync(ct);
    }
}
