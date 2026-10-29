using System.Globalization;
using System.Net;
using Application.Common;
using Application.Invites;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Notifications;

internal sealed class InviteNotifier(
    IEmailSender email,
    IOptions<AppSettings> app,
    ILogger<InviteNotifier> logger) : IInviteNotifier
{
    public string CreateLink(string token)
        => $"{app.Value.FrontendBaseUrl.TrimEnd('/')}/register?token={Uri.EscapeDataString(token)}";

    public async Task<bool> TrySendInviteAsync(string to, string link, DateTime expiresAtUtc, CancellationToken ct)
    {
        if (!email.CanDeliver) return false;

        var expires = expiresAtUtc.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
        var text =
            $"""
             You've been invited to Solicitations.

             Create your account here (link valid until {expires}):
             {link}

             If you weren't expecting this, you can ignore this email.
             """;
        var safeLink = WebUtility.HtmlEncode(link);
        var html =
            $"""
             <div style="font-family:-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;font-size:14px;color:#16171a;max-width:480px">
               <p>You've been invited to <strong>Solicitations</strong>.</p>
               <p><a href="{safeLink}" style="display:inline-block;background:#5e6ad2;color:#fff;padding:10px 16px;border-radius:6px;text-decoration:none;font-weight:600">Create your account</a></p>
               <p style="color:#6b7178">The link is valid until {expires}. If the button doesn't work, paste this into your browser:<br><span style="word-break:break-all">{safeLink}</span></p>
               <p style="color:#6b7178">If you weren't expecting this, you can ignore this email.</p>
             </div>
             """;

        try
        {
            await email.SendAsync(new EmailMessage(to, "You've been invited to Solicitations", text, html), ct);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The invite exists either way; the admin can copy the link from the UI.
            logger.LogError(ex, "Failed to send invite email to {To}", to);
            return false;
        }
    }
}