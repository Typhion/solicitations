namespace Application.Common;

public interface ISecureTokenService
{
    string Generate();
    string Hash(string token);
}