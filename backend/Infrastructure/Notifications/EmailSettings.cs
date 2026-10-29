using MailKit.Security;

namespace Infrastructure.Notifications;

/// <summary>
/// SMTP delivery (any transactional provider: Brevo, Postmark, Resend, Mailgun, SES…, or a local
/// catcher like Mailpit in dev). Leave <see cref="Host"/> empty to disable sending.
/// <c>Email__Password</c> is a secret: user-secrets in dev, server .env in prod.
/// </summary>
public sealed class EmailSettings
{
    public const string SectionName = "Email";

    public string? Host { get; init; }
    public int Port { get; init; } = 587;
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string FromAddress { get; init; } = default!;
    public string FromName { get; init; } = "Solicitations";

    /// <summary>Auto: implicit TLS on 465, STARTTLS when offered otherwise.</summary>
    public SecureSocketOptions Security { get; init; } = SecureSocketOptions.Auto;

    /// <summary>
    /// Off by default: the CRL/OCSP lookup often can't complete (e.g. on macOS, or for CAs that
    /// dropped OCSP), which makes every send fail with "incomplete certificate revocation check".
    /// The certificate chain and host name are still validated either way.
    /// </summary>
    public bool CheckCertificateRevocation { get; init; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Host);
}

/// <summary>Where the SPA lives, for links in emails (e.g. https://admin.typhion.com).</summary>
public sealed class AppSettings
{
    public const string SectionName = "App";

    public string FrontendBaseUrl { get; init; } = default!;
}