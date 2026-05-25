namespace TicketPlatform.Business.Payments;

public class MockPaymentProcessor : IPaymentProcessor
{
    public Task<PaymentResult> ChargeAsync(PaymentRequest request, CancellationToken ct)
    {
        var txId = $"MOCK-{Guid.NewGuid():N}";
        return Task.FromResult(new PaymentResult(true, txId, null));
    }
}
