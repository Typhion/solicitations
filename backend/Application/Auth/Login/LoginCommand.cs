using MediatR;

namespace Application.Auth.Login;

public sealed record LoginCommand(string Username, string Password) : IRequest<LoginResult>;