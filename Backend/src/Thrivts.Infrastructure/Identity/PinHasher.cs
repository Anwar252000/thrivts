using System.Security.Cryptography;
using Thrivts.Application.Common.Interfaces;

namespace Thrivts.Infrastructure.Identity;

/// <summary>PBKDF2-HMAC-SHA256 with a per-PIN random salt and 600,000 iterations (the OWASP
/// recommendation). Stored as "v1$iterations$salt$hash" so the work factor can be raised later
/// without invalidating existing PINs. Comparison is constant-time.</summary>
public sealed class PinHasher : IPinHasher
{
    private const int Iterations = 600_000;
    private const int SaltBytes = 16;
    private const int HashBytes = 32;

    public string Hash(string pin)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(pin, salt, Iterations, HashAlgorithmName.SHA256, HashBytes);
        return $"v1${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public bool Verify(string hash, string pin)
    {
        var parts = hash.Split('$');
        if (parts.Length != 4 || parts[0] != "v1" || !int.TryParse(parts[1], out var iterations))
            return false;

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(pin, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
