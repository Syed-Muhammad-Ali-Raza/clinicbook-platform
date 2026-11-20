namespace ClinicBook.Infrastructure.Identity;

/// <summary>
/// Issues and validates JWT access tokens.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>Creates an access token for the specified user.</summary>
    /// <param name="user">Application user.</param>
    /// <param name="roles">User roles.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Access token value and expiry.</returns>
    Task<(string Token, DateTime ExpiresAtUtc)> CreateAccessTokenAsync(ApplicationUser user, IEnumerable<string> roles, CancellationToken ct = default);
}
