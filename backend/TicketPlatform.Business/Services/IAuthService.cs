using TicketPlatform.Data.Entities;

namespace TicketPlatform.Business.Services;

public record AuthResult(int UserId, string Email, UserRole Role);

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(string email, string password, CancellationToken ct);
    Task<AuthResult?> AuthenticateAsync(string email, string password, CancellationToken ct);
}
