using Microsoft.EntityFrameworkCore;
using TicketPlatform.Business.Background;
using TicketPlatform.Business.Payments;
using TicketPlatform.Business.Pricing;
using TicketPlatform.Data;
using TicketPlatform.Data.Entities;

namespace TicketPlatform.Business.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    private readonly IPricingStrategyFactory _pricing;
    private readonly IPaymentProcessor _payments;
    private readonly IEmailQueue _emails;

    public OrderService(
        AppDbContext db,
        IPricingStrategyFactory pricing,
        IPaymentProcessor payments,
        IEmailQueue emails)
    {
        _db = db;
        _pricing = pricing;
        _payments = payments;
        _emails = emails;
    }

    public async Task<Order> PlaceOrderAsync(
        int userId, string userEmail, OrderInput input, CancellationToken ct)
    {
        if (input.Items.Count == 0)
            throw new InvalidOperationException("Order must contain at least one item.");

        var categoryIds = input.Items.Select(i => i.TicketCategoryId).ToList();
        var categories = await _db.TicketCategories
            .Include(c => c.Event)
            .Where(c => categoryIds.Contains(c.Id))
            .ToListAsync(ct);

        if (categories.Count != categoryIds.Distinct().Count())
            throw new InvalidOperationException("Some ticket categories were not found.");

        var order = new Order { UserId = userId, Status = OrderStatus.Pending };
        decimal total = 0m;

        foreach (var item in input.Items)
        {
            var cat = categories.Single(c => c.Id == item.TicketCategoryId);
            if (cat.SoldQuantity + item.Quantity > cat.TotalQuantity)
                throw new InvalidOperationException($"Not enough tickets in '{cat.Name}'.");

            var strategy = _pricing.For(cat.Event!.PricingStrategy);
            var unitPrice = strategy.CalculatePrice(cat.BasePrice, cat.Event);

            cat.SoldQuantity += item.Quantity;
            order.Items.Add(new OrderItem
            {
                TicketCategoryId = cat.Id,
                Quantity = item.Quantity,
                UnitPrice = unitPrice
            });
            total += unitPrice * item.Quantity;
        }

        order.TotalAmount = total;
        _db.Orders.Add(order);

        // First SaveChanges: reserves seats and creates the pending order.
        // Optimistic locking on TicketCategory.Xmin catches concurrent edits to the same category.
        await _db.SaveChangesAsync(ct);

        // Synchronous payment call inside the HTTP request — short, deterministic.
        var paymentResult = await _payments.ChargeAsync(
            new PaymentRequest(order.Id, userId, total), ct);

        order.Status = paymentResult.Success ? OrderStatus.Paid : OrderStatus.Failed;
        if (!paymentResult.Success)
            // Refund seats so they're not held forever.
            foreach (var item in order.Items)
            {
                var cat = categories.Single(c => c.Id == item.TicketCategoryId);
                cat.SoldQuantity -= item.Quantity;
            }
        await _db.SaveChangesAsync(ct);

        // Async/non-blocking: queue email send, do NOT await SMTP. Browser gets the response immediately.
        if (paymentResult.Success)
            await _emails.EnqueueAsync(new EmailJob(
                userEmail,
                $"Order #{order.Id} confirmed",
                $"Thank you! Your order total was {total:0.00}."), ct);

        return order;
    }

    public async Task<IReadOnlyList<Order>> GetForUserAsync(int userId, CancellationToken ct) =>
        await _db.Orders
            .Where(o => o.UserId == userId)
            .Include(o => o.Items).ThenInclude(i => i.TicketCategory).ThenInclude(c => c!.Event)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);
}
