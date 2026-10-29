namespace Application.Invites;

/// <summary>Builds invite links and delivers the invite email.</summary>
public interface IInviteNotifier
{
    /// <summary>Absolute URL of the SPA's register page for this raw token.</summary>
    string CreateLink(string token);

    /// <summary>
    /// Sends the invite. Never throws for delivery problems: returns false when email isn't
    /// configured or sending failed (logged), so the invite itself still succeeds.
    /// </summary>
    Task<bool> TrySendInviteAsync(string email, string link, DateTime expiresAtUtc, CancellationToken ct);
}