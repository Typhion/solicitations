using Application.Auth.Login;

namespace Application.Auth;

public interface IAuthenticationService
{
    Task<LoginResult> LoginAsync(string username, string password, CancellationToken ct);
}