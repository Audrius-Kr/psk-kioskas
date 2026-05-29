using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketPlatform.Api.Dtos;
using TicketPlatform.Business.Services;

namespace TicketPlatform.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders) { _orders = orders; }

    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Place([FromBody] PlaceOrderRequest req, CancellationToken ct)
    {
        try
        {
            var order = await _orders.PlaceOrderAsync(
                CurrentUserId, CurrentUserEmail,
                new OrderInput(req.Items.Select(i => new OrderItemInput(i.TicketCategoryId, i.Quantity)).ToList()),
                ct);
            return Ok(ToDto(order));
        }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new
            {
                error = "concurrency_conflict",
                message = "Ticket availability changed while placing your order. Please refresh and try again."
            });
        }
    }

    [HttpGet("mine")]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> Mine(CancellationToken ct)
    {
        var orders = await _orders.GetForUserAsync(CurrentUserId, ct);
        return Ok(orders.Select(ToDto));
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string CurrentUserEmail => User.FindFirstValue(ClaimTypes.Email) ?? "";

    private static OrderResponse ToDto(TicketPlatform.Data.Entities.Order o) => new(
        o.Id, o.Status, o.TotalAmount, o.CreatedAt,
        o.Items.Select(i => new OrderItemResponse(
            i.TicketCategoryId,
            i.TicketCategory?.Name ?? "",
            i.TicketCategory?.Event?.Title ?? "",
            i.Quantity,
            i.UnitPrice)).ToList());
}
