namespace TicketPlatform.Data.Entities;

public class TicketCategory
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public Event? Event { get; set; }

    public string Name { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public int TotalQuantity { get; set; }
    public int SoldQuantity { get; set; }

    public uint Xmin { get; set; }
}
