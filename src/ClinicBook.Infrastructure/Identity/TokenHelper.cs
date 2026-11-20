using System.Security.Cryptography;
using System.Text;

namespace ClinicBook.Infrastructure.Identity;

/// <summary>
/// Helpers for token generation and hashing.
/// </summary>
public static class TokenHelper
{
    /// <summary>Generates a cryptographically secure opaque token (URL-safe base64).</summary>
    /// <param name="byteLength">Number of random bytes.</param>
    /// <returns>Token string.</returns>
    public static string GenerateSecureToken(int byteLength = 64)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        return Base64UrlEncode(bytes);
    }

    /// <summary>Computes a SHA-256 hash of a token for storage.</summary>
    /// <param name="token">Raw token value.</param>
    /// <returns>Lowercase hex hash.</returns>
    public static string HashToken(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>Generates a random CSRF token.</summary>
    /// <returns>Token string.</returns>
    public static string GenerateCsrfToken() => GenerateSecureToken(32);

    /// <summary>Performs a fixed-time comparison for tokens.</summary>
    /// <param name="left">First value.</param>
    /// <param name="right">Second value.</param>
    /// <returns><c>true</c> if equal.</returns>
    public static bool FixedTimeEquals(string? left, string? right)
    {
        if (left is null || right is null)
        {
            return false;
        }

        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
