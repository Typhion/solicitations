namespace Application.Users;

public sealed record UserResponse(Guid Id, string Username, string? Email, IReadOnlyList<string> Roles, bool IsLockedOut);

public sealed record UserSummary(Guid Id, string Username, string? Email, IReadOnlyList<string> Roles, bool IsLockedOut);