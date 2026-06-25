namespace Infrastructure.Security;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";
    public string Issuer { get; init; } = default!;
    public string Audience { get; init; } = default!;
    public string Key { get; init; } = default!;
    public int ExpiryMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 14;
}