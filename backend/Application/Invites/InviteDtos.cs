using Domain.Invites;

namespace Application.Invites;

/// <summary>Invites always create a <see cref="Common.Roles.Member"/>; admins are promoted afterwards.</summary>
public sealed record CreateInviteRequest(string Email, int? ExpiresInDays);

/// <param name="Link">Register link containing the raw token — only ever returned here, never stored.</param>
/// <param name="EmailSent">False when delivery isn't configured or failed; the admin can share the link instead.</param>
public sealed record CreatedInviteResponse(Guid Id, string Link, DateTime ExpiresAtUtc, bool EmailSent);

public sealed record InviteResponse(
    Guid Id,
    string? Email,
    string Role,
    InviteStatus Status,
    bool IsExpired,
    DateTime ExpiresAtUtc,
    DateTime CreatedAtUtc);

/// <summary>What the register page may know about a valid invite token.</summary>
public sealed record InviteLookupResponse(string? Email, DateTime ExpiresAtUtc);