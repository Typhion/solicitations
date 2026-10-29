using Application.Common;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Notifications;

/// <summary>
/// Fallback when no SMTP server is configured (dev/tests). Logs only recipient + subject —
/// bodies can contain invite links, which must not end up in logs.
/// </summary>
internal sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public bool CanDeliver => false;

    public Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        logger.LogInformation("Email delivery not configured; not sending \"{Subject}\" to {To}", message.Subject, message.To);
        return Task.CompletedTask;
    }
}
