using TicketPlatform.Data.Entities;

namespace TicketPlatform.Business.Pricing;

public interface IPricingStrategyFactory
{
    IPricingStrategy For(PricingStrategyType type);
}

public class PricingStrategyFactory : IPricingStrategyFactory
{
    private readonly Dictionary<PricingStrategyType, IPricingStrategy> _strategies;

    public PricingStrategyFactory(IEnumerable<IPricingStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.Type);
    }

    public IPricingStrategy For(PricingStrategyType type) =>
        _strategies.TryGetValue(type, out var s)
            ? s
            : throw new InvalidOperationException($"No pricing strategy registered for {type}");
}
