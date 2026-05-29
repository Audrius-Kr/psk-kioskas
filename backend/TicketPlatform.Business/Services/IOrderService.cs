using TicketPlatform.Data.Entities;

namespace TicketPlatform.Business.Services;

public record OrderItemInput(int TicketCategoryId, int Quantity);
public record OrderInput(IReadOnlyList<OrderItemInput> Items);

public interface IOrderService
{
    Task<Order> PlaceOrderAsync(int userId, string userEmail, OrderInput input, CancellationToken ct);
    Task<IReadOnlyList<Order>> GetForUserAsync(int userId, CancellationToken ct);
}
