using System.Security.Cryptography;
using System.Text;
using Application.Invites;

namespace Infrastructure.Security;

internal sealed class InviteTokenService : IInviteTokenService
{
    public string Generate() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    public string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}