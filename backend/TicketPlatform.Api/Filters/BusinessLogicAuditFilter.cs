using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TicketPlatform.Api.Filters;

// Cross-cutting audit/logging filter (Interceptor pattern).
// Records user, roles, time, and the invoked controller.action for every action.
// Toggleable via appsettings "BusinessLogic:AuditLogging" — no recompile needed.
public class BusinessLogicAuditFilter : IAsyncActionFilter
{
    private readonly ILogger<BusinessLogicAuditFilter> _logger;

    public BusinessLogicAuditFilter(ILogger<BusinessLogicAuditFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        var userName = user?.Identity?.Name ?? "anonymous";
        var roles = user is null
            ? "-"
            : string.Join(",", user.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value));
        var action = context.ActionDescriptor as ControllerActionDescriptor;
        var location = action is null
            ? context.ActionDescriptor.DisplayName ?? "?"
            : $"{action.ControllerName}Controller.{action.ActionName}";

        var startedAt = DateTime.UtcNow;
        _logger.LogInformation(
            "[audit] {Time:O} user={User} roles=[{Roles}] method={Method}",
            startedAt, userName, roles, location);

        var executed = await next();

        var elapsedMs = (DateTime.UtcNow - startedAt).TotalMilliseconds;
        _logger.LogInformation(
            "[audit] {Time:O} user={User} method={Method} elapsedMs={Elapsed:F1} exception={Ex}",
            DateTime.UtcNow, userName, location, elapsedMs, executed.Exception?.GetType().Name ?? "-");
    }
}
