namespace TicketPlatform.Data.Entities;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int TicketCategoryId { get; set; }
    public TicketCategory? TicketCategory { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
