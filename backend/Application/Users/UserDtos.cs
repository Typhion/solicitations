namespace Application.Users;

public sealed record UserResponse(
    Guid Id,
    string Username,
    string? Email,
    IReadOnlyList<string> Roles,
    bool IsLockedOut,
    bool TwoFactorEnabled,
    int SolicitationCount);

public sealed record UserSummary(
    Guid Id, string Username, string? Email, IReadOnlyList<string> Roles, bool IsLockedOut, bool TwoFactorEnabled);

/// <summary>
/// Deleting a user is a step-up action: the admin re-types the target's username and proves
/// presence with their own authenticator code.
/// </summary>
public sealed record DeleteUserRequest(string Username, string Code);