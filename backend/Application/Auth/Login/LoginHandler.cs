using MediatR;

namespace Application.Auth.Login;

internal sealed class LoginHandler(IAuthenticationService auth) : IRequestHandler<LoginCommand, LoginResult>
{
    public Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
        => auth.LoginAsync(request.Username, request.Password, cancellationToken);
}
