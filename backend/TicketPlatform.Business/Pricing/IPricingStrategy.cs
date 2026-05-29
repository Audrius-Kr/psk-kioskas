using TicketPlatform.Data.Entities;

namespace TicketPlatform.Business.Pricing;

// Strategy pattern (extensibility requirement).
// Each pricing rule is its own class. Adding a new rule = new class, no edits to existing code.
public interface IPricingStrategy
{
    PricingStrategyType Type { get; }
    decimal CalculatePrice(decimal basePrice, Event @event);
}
