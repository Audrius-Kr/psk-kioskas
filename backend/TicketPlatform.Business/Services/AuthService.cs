using Microsoft.EntityFrameworkCore;
using TicketPlatform.Data;
using TicketPlatform.Data.Entities;

namespace TicketPlatform.Business.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;

    public AuthService(AppDbContext db) { _db = db; }

    public async Task<AuthResult> RegisterAsync(string email, string password, CancellationToken ct)
    {
        email = email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            throw new InvalidOperationException("Email already registered.");

        // The very first registered user becomes Admin so the demo has someone who can create events.
        var role = await _db.Users.AnyAsync(ct) ? UserRole.Customer : UserRole.Admin;
        var user = new User
        {
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return new AuthResult(user.Id, user.Email, user.Role);
    }

    public async Task<AuthResult?> AuthenticateAsync(string email, string password, CancellationToken ct)
    {
        email = email.Trim().ToLowerInvariant();
        // Parameterized LINQ query — EF generates a prepared statement, immune to SQL injection.
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == email, ct);
        if (user is null) return null;
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;
        return new AuthResult(user.Id, user.Email, user.Role);
    }
}
