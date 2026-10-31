using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Infrastructure.Interfaces;
using Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Security;

internal sealed class JwtTokenService(IOptions<JwtSettings> options) : IJwtTokenService
{
    private readonly JwtSettings _settings = options.Value;

    public (string Token, DateTime ExpiresAt) CreateToken(AppUser user, IEnumerable<string> roles)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            // Checked against the database on every request (see AccessTokenValidator), so
            // password/role/2FA changes invalidate tokens issued before them.
            new(SecurityStampClaim, user.SecurityStamp ?? string.Empty)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        // OIDC standard claim; lets the SPA apply the saved language on startup without another request.
        if (!string.IsNullOrEmpty(user.Language)) claims.Add(new Claim("locale", user.Language));

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(_settings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    internal const string SecurityStampClaim = "ss";
    private static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(5);

    // Distinct audience: the JwtBearer handler only accepts _settings.Audience, so a
    // challenge can't be replayed as an access token.
    private string ChallengeAudience => $"{_settings.Audience}:2fa";

    public string CreateTwoFactorChallenge(Guid userId, string securityStamp)
    {
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: ChallengeAudience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(SecurityStampClaim, securityStamp),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            ],
            expires: DateTime.UtcNow.Add(ChallengeLifetime),
            signingCredentials: new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (Guid UserId, string SecurityStamp)? ReadTwoFactorChallenge(string challengeToken)
    {
        // MapInboundClaims = false keeps "sub"/"ss" as-is instead of mapping to long URI claim types.
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        try
        {
            var principal = handler.ValidateToken(challengeToken, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _settings.Issuer,
                ValidAudience = ChallengeAudience,
                IssuerSigningKey = SigningKey,
                ClockSkew = TimeSpan.FromSeconds(30)
            }, out _);

            var stamp = principal.FindFirstValue(SecurityStampClaim);
            return Guid.TryParse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId) && stamp is not null
                ? (userId, stamp)
                : null;
        }
        catch (SecurityTokenException)
        {
            return null;
        }
        catch (ArgumentException)
        {
            return null; // malformed token
        }
    }

    private SymmetricSecurityKey SigningKey => new(System.Text.Encoding.UTF8.GetBytes(_settings.Key));
}