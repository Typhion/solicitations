using System.Security.Cryptography;
using System.Text;
using Application.Common;

namespace Infrastructure.Security;

internal sealed class SecureTokenService : ISecureTokenService
{
    public string Generate() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    public string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}