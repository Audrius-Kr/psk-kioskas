using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketPlatform.Api.Dtos;
using TicketPlatform.Business.Pricing;
using TicketPlatform.Business.Services;
using TicketPlatform.Data.Entities;
using TicketPlatform.Data.Repositories;

namespace TicketPlatform.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _events;
    private readonly IEventSearchRepository _search;
    private readonly IPricingStrategyFactory _pricing;

    public EventsController(IEventService events, IEventSearchRepository search, IPricingStrategyFactory pricing)
    {
        _events = events;
        _search = search;
        _pricing = pricing;
    }

    // Anonymous list — uses the Dapper-backed read repository (Data Mapper).
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EventListItemDto>>> List([FromQuery] string? q, CancellationToken ct)
    {
        var rows = await _search.SearchAsync(q, ct);
        return Ok(rows.Select(r => new EventListItemDto
        {
            Id = r.Id, Title = r.Title, Venue = r.Venue,
            EventDate = r.EventDate, MinPrice = r.MinPrice, AvailableTickets = r.AvailableTickets
        }));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EventDto>> Get(int id, CancellationToken ct)
    {
        var ev = await _events.GetAsync(id, ct);
        if (ev is null) return NotFound();
        return Ok(ToDto(ev));
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<EventDto>> Create([FromBody] EventDto dto, CancellationToken ct)
    {
        var ev = await _events.CreateAsync(MapInput(dto), ct);
        return CreatedAtAction(nameof(Get), new { id = ev.Id }, ToDto(ev));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<EventDto>> Update(int id, [FromBody] EventDto dto, CancellationToken ct)
    {
        try
        {
            var ev = await _events.UpdateAsync(id, dto.Xmin, MapInput(dto), ct);
            return Ok(ToDto(ev));
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (DbUpdateConcurrencyException)
        {
            // Optimistic-locking conflict: another tab/admin wrote first. Return current server state
            // so the client can show a "data changed — reload or overwrite" dialog.
            var current = await _events.GetAsync(id, ct);
            return Conflict(new
            {
                error = "concurrency_conflict",
                message = "This event was modified by someone else. Reload to see the latest version, then re-apply your changes.",
                current = current is null ? null : ToDto(current)
            });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _events.DeleteAsync(id, ct);
        return NoContent();
    }

    private EventInput MapInput(EventDto dto) => new(
        dto.Title, dto.Venue, dto.Description, dto.EventDate, dto.PricingStrategy,
        dto.Categories.Select(c => new EventInputCategory(c.Id, c.Name, c.BasePrice, c.TotalQuantity, c.Xmin)).ToList());

    private EventDto ToDto(Event ev)
    {
        var strategy = _pricing.For(ev.PricingStrategy);
        return new EventDto
        {
            Id = ev.Id,
            Title = ev.Title,
            Venue = ev.Venue,
            Description = ev.Description,
            EventDate = ev.EventDate,
            PricingStrategy = ev.PricingStrategy,
            Xmin = ev.Xmin,
            Categories = ev.Categories.Select(c => new TicketCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                BasePrice = c.BasePrice,
                TotalQuantity = c.TotalQuantity,
                SoldQuantity = c.SoldQuantity,
                EffectivePrice = strategy.CalculatePrice(c.BasePrice, ev),
                Xmin = c.Xmin
            }).ToList()
        };
    }
}
