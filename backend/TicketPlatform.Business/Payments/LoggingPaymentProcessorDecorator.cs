using Microsoft.Extensions.Logging;

namespace TicketPlatform.Business.Payments;

// Decorator pattern (extensibility requirement).
// Wraps any IPaymentProcessor with structured logging — toggle via config without recompile.
public class LoggingPaymentProcessorDecorator : IPaymentProcessor
{
    private readonly IPaymentProcessor _inner;
    private readonly ILogger<LoggingPaymentProcessorDecorator> _logger;

    public LoggingPaymentProcessorDecorator(
        IPaymentProcessor inner,
        ILogger<LoggingPaymentProcessorDecorator> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct)
    {
        _logger.LogInformation(
            "Payment.Charge START order={OrderId} user={UserId} amount={Amount}",
            request.OrderId, request.UserId, request.Amount);

        var result = await _inner.ChargeAsync(request, ct);

        _logger.LogInformation(
            "Payment.Charge END   order={OrderId} success={Success} tx={Tx}",
            request.OrderId, result.Success, result.TransactionId);

        return result;
    }
}
