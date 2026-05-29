using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using TicketPlatform.Api.Dtos;
using TicketPlatform.Business.Services;

namespace TicketPlatform.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly IConfiguration _config;

    public AuthController(IAuthService auth, IConfiguration config)
    {
        _auth = auth;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req, CancellationToken ct)
    {
        try
        {
            var u = await _auth.RegisterAsync(req.Email, req.Password, ct);
            return Ok(IssueToken(u));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var u = await _auth.AuthenticateAsync(req.Email, req.Password, ct);
        if (u is null) return Unauthorized(new { error = "Invalid credentials" });
        return Ok(IssueToken(u));
    }

    private AuthResponse IssueToken(AuthResult u)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var minutes = _config.GetValue<int>("Jwt:ExpiryMinutes");
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, u.UserId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, u.UserId.ToString()),
            new Claim(ClaimTypes.Name, u.Email),
            new Claim(ClaimTypes.Email, u.Email),
            new Claim(ClaimTypes.Role, u.Role.ToString())
        };
        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: creds);
        return new AuthResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            u.UserId, u.Email, u.Role.ToString());
    }
}
