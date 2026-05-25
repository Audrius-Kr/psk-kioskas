using TicketPlatform.Data.Entities;

namespace TicketPlatform.Business.Pricing;

public class EarlyBirdPricingStrategy : IPricingStrategy
{
    private const decimal DiscountThresholdDays = 30m;
    private const decimal Discount = 0.20m;

    public PricingStrategyType Type => PricingStrategyType.EarlyBird;

    public decimal CalculatePrice(decimal basePrice, Event @event)
    {
        var daysUntil = (decimal)(@event.EventDate - DateTime.UtcNow).TotalDays;
        return daysUntil >= DiscountThresholdDays
            ? Math.Round(basePrice * (1m - Discount), 2)
            : basePrice;
    }
}
