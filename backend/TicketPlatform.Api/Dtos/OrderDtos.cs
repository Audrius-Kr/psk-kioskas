using System.ComponentModel.DataAnnotations;
using TicketPlatform.Data.Entities;

namespace TicketPlatform.Api.Dtos;

public class OrderItemRequest
{
    [Required] public int TicketCategoryId { get; set; }
    [Range(1, 100)] public int Quantity { get; set; }
}

public class PlaceOrderRequest
{
    [Required, MinLength(1)] public List<OrderItemRequest> Items { get; set; } = new();
}

public record OrderItemResponse(int TicketCategoryId, string TicketCategoryName, string EventTitle, int Quantity, decimal UnitPrice);
public record OrderResponse(int Id, OrderStatus Status, decimal TotalAmount, DateTime CreatedAt, List<OrderItemResponse> Items);
