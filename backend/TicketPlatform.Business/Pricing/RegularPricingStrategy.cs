using TicketPlatform.Data.Entities;

namespace TicketPlatform.Business.Pricing;

public class RegularPricingStrategy : IPricingStrategy
{
    public PricingStrategyType Type => PricingStrategyType.Regular;

    public decimal CalculatePrice(decimal basePrice, Event @event) => basePrice;
}
