namespace Application.Common;

public interface IUserRegistrar
{
    Task<UserRegistrationResult> CreateAsync(string username, string email, string password, string role, CancellationToken ct);
}
public sealed record UserRegistrationResult(bool Succeeded, IReadOnlyList<string> Errors);