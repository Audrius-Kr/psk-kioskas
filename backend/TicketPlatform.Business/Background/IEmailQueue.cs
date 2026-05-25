using System.Threading.Channels;

namespace TicketPlatform.Business.Background;

public record EmailJob(string To, string Subject, string Body);

public interface IEmailQueue
{
    ValueTask EnqueueAsync(EmailJob job, CancellationToken ct = default);
    IAsyncEnumerable<EmailJob> ReadAllAsync(CancellationToken ct);
}

public class InMemoryEmailQueue : IEmailQueue
{
    private readonly Channel<EmailJob> _channel =
        Channel.CreateUnbounded<EmailJob>(new UnboundedChannelOptions { SingleReader = true });

    public ValueTask EnqueueAsync(EmailJob job, CancellationToken ct = default) =>
        _channel.Writer.WriteAsync(job, ct);

    public IAsyncEnumerable<EmailJob> ReadAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
