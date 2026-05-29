namespace TicketPlatform.Data.Entities;

public enum PricingStrategyType
{
    Regular = 0,
    EarlyBird = 1
}

public class Event
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public PricingStrategyType PricingStrategy { get; set; } = PricingStrategyType.Regular;

    // PostgreSQL xmin used as the concurrency token (optimistic locking).
    public uint Xmin { get; set; }

    public List<TicketCategory> Categories { get; set; } = new();
}
