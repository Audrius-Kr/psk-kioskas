namespace TicketPlatform.Business.Payments;

public record PaymentRequest(int OrderId, int UserId, decimal Amount);
public record PaymentResult(bool Success, string TransactionId, string? Error);

public interface IPaymentProcessor
{
    Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct);
}
