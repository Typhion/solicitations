namespace Application.Auth;

public sealed record RegisterResult(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static RegisterResult Success() => new(true, []);
    public static RegisterResult Fail(string error) => new(false, [error]);
    public static RegisterResult Fail(IReadOnlyList<string> errors) => new(false, errors);
}
