namespace Application.Auth;

public sealed record RegisterRequest(string Token, string Username, string Email, string Password);
