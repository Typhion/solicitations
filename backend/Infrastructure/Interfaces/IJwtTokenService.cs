using Infrastructure.Persistence;

namespace Infrastructure.Interfaces;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) CreateToken(AppUser user, IEnumerable<string> roles);
}