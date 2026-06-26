using Application.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Notifications;

/// <summary>
/// Default email "sender" that just logs the message.
/// Swap for a real SMTP/provider implementation (same interface) when you wire up delivery.
/// </summary>
internal sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken ct)
    {
        logger.LogInformation("EMAIL → {To} | {Subject} | {Body}", to, subject, body);
        return Task.CompletedTask;
    }
}
