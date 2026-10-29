namespace Application.Common;

public sealed record EmailMessage(string To, string Subject, string TextBody, string? HtmlBody = null);

public interface IEmailSender
{
    /// <summary>False when no real delivery is configured (messages would only be logged).</summary>
    bool CanDeliver { get; }

    Task SendAsync(EmailMessage message, CancellationToken ct);
}
