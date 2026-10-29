using System.Security.Cryptography;

namespace Integration.Tests;

/// <summary>
/// Minimal RFC 6238 TOTP (SHA-1, 6 digits, 30 s) — what an authenticator app computes from the
/// base32 secret in the otpauth:// URI. Lets tests act as the user's phone.
/// </summary>
internal static class Totp
{
    public static string Compute(string base32Secret)
    {
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian) Array.Reverse(counterBytes);

        var hash = HMACSHA1.HashData(Base32Decode(base32Secret), counterBytes);
        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
        return (binary % 1_000_000).ToString("D6");
    }

    private static byte[] Base32Decode(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bytes = new List<byte>();
        int buffer = 0, bits = 0;
        foreach (var c in input.TrimEnd('=').ToUpperInvariant())
        {
            buffer = (buffer << 5) | alphabet.IndexOf(c);
            bits += 5;
            if (bits >= 8)
            {
                bytes.Add((byte)(buffer >> (bits - 8)));
                bits -= 8;
            }
        }
        return bytes.ToArray();
    }
}