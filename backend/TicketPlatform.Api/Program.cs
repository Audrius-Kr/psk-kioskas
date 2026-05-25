using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TicketPlatform.Api.Filters;
using TicketPlatform.Business.Background;
using TicketPlatform.Business.Payments;
using TicketPlatform.Business.Pricing;
using TicketPlatform.Business.Services;
using TicketPlatform.Data;
using TicketPlatform.Data.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ---------- Data layer ----------
var connectionString = builder.Configuration.GetConnectionString("Postgres")!;
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseNpgsql(connectionString));
builder.Services.AddSingleton<IEventSearchRepository>(_ => new EventSearchRepository(connectionString));

// ---------- Business layer ----------
// All use-case services are Scoped (per HTTP request). No SessionScoped anywhere.
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// Strategy pattern: pricing strategies. Adding a new strategy = new class registered here.
builder.Services.AddScoped<IPricingStrategy, RegularPricingStrategy>();
builder.Services.AddScoped<IPricingStrategy, EarlyBirdPricingStrategy>();
builder.Services.AddScoped<IPricingStrategyFactory, PricingStrategyFactory>();

// Decorator pattern: payment processor wrapped by logging decorator (toggle in config).
var enableLoggingDecorator = builder.Configuration.GetValue<bool>("Payments:EnableLoggingDecorator");
if (enableLoggingDecorator)
{
    builder.Services.AddScoped<MockPaymentProcessor>();
    builder.Services.AddScoped<IPaymentProcessor>(sp =>
        new LoggingPaymentProcessorDecorator(
            sp.GetRequiredService<MockPaymentProcessor>(),
            sp.GetRequiredService<ILogger<LoggingPaymentProcessorDecorator>>()));
}
else
{
    builder.Services.AddScoped<IPaymentProcessor, MockPaymentProcessor>();
}

// Background email queue (async/non-blocking demo): controller returns immediately,
// the email "send" happens on a hosted service.
builder.Services.AddSingleton<IEmailQueue, InMemoryEmailQueue>();
builder.Services.AddHostedService<EmailBackgroundService>();

// ---------- API layer ----------
// Action filter implements cross-cutting business-logic logging (Interceptor pattern).
// Toggleable via appsettings without recompile.
builder.Services.AddScoped<BusinessLogicAuditFilter>();
builder.Services.AddControllers(o =>
{
    if (builder.Configuration.GetValue<bool>("BusinessLogic:AuditLogging"))
        o.Filters.AddService<BusinessLogicAuditFilter>();
});

// JWT auth — stateless, no server session. Same account works in multiple tabs/windows.
var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

// CORS for the React dev server.
var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(origins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
