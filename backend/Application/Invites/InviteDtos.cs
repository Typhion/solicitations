using Domain.Invites;

namespace Application.Invites;

public sealed record CreateInviteRequest(string Role, string? Email, int? ExpiresInDays);

public sealed record CreatedInviteResponse(Guid Id, string Token, DateTime ExpiresAtUtc);

public sealed record InviteResponse(
    Guid Id,
    string? Email,
    string Role,
    InviteStatus Status,
    DateTime ExpiresAtUtc,
    DateTime CreatedAtUtc);