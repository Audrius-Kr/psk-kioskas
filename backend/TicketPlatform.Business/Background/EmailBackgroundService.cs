using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TicketPlatform.Business.Background;

// Long-running operation runs off the HTTP request thread. Browser is never blocked waiting.
public class EmailBackgroundService : BackgroundService
{
    private readonly IEmailQueue _queue;
    private readonly ILogger<EmailBackgroundService> _logger;

    public EmailBackgroundService(IEmailQueue queue, ILogger<EmailBackgroundService> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                // Simulate slow external work (SMTP, third-party API, etc.)
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                _logger.LogInformation(
                    "[email-sent] to={To} subject={Subject}", job.To, job.Subject);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process email job for {To}", job.To);
            }
        }
    }
}
