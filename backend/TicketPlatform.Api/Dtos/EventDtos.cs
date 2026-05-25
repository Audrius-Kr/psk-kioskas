using System.ComponentModel.DataAnnotations;
using TicketPlatform.Data.Entities;

namespace TicketPlatform.Api.Dtos;

public class TicketCategoryDto
{
    public int? Id { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [Range(0, 100000)] public decimal BasePrice { get; set; }
    [Range(0, 1_000_000)] public int TotalQuantity { get; set; }
    public int SoldQuantity { get; set; }
    public decimal? EffectivePrice { get; set; }
    public uint? Xmin { get; set; }
}

public class EventDto
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Venue { get; set; } = string.Empty;
    [MaxLength(2000)] public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public PricingStrategyType PricingStrategy { get; set; }
    public uint Xmin { get; set; }
    public List<TicketCategoryDto> Categories { get; set; } = new();
}

public class EventListItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Venue { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public decimal? MinPrice { get; set; }
    public int AvailableTickets { get; set; }
}
