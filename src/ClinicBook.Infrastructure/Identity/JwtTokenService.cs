using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ClinicBook.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ClinicBook.Infrastructure.Identity;

/// <summary>
/// JWT access token service.
/// </summary>
public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;
    private readonly IClock _clock;

    /// <summary>Initializes a new instance of the <see cref="JwtTokenService"/> class.</summary>
    /// <param name="options">JWT options.</param>
    /// <param name="clock">Clock abstraction.</param>
    public JwtTokenService(IOptions<JwtOptions> options, IClock clock)
    {
        _options = options.Value;
        _clock = clock;
    }

    /// <inheritdoc />
    public Task<(string Token, DateTime ExpiresAtUtc)> CreateAccessTokenAsync(ApplicationUser user, IEnumerable<string> roles, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var signingKey = GetSigningKey();
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var nowUtc = DateTime.SpecifyKind(_clock.UtcNow, DateTimeKind.Utc);
        var expiresUtc = nowUtc.AddMinutes(Math.Max(1, _options.AccessTokenMinutes));
        var jti = Guid.NewGuid().ToString("N");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, jti),
            new("token_use", "access")
        };

        foreach (var role in roles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        if (user.ClinicId.HasValue)
        {
            claims.Add(new Claim("clinic_id", user.ClinicId.Value.ToString()));
        }

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: nowUtc,
            expires: expiresUtc,
            signingCredentials: credentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return Task.FromResult((tokenString, expiresUtc));
    }

    private SymmetricSecurityKey GetSigningKey()
    {
        if (!string.IsNullOrWhiteSpace(_options.KeyPath) && File.Exists(_options.KeyPath))
        {
            var keyText = File.ReadAllText(_options.KeyPath).Trim();
            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyText));
        }

        if (!string.IsNullOrWhiteSpace(_options.SigningKey))
        {
            if (_options.SigningKey.Length < 32)
            {
                throw new InvalidOperationException("Jwt:SigningKey must be at least 32 characters.");
            }

            return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        }

        throw new InvalidOperationException("JWT signing key is not configured. Set Jwt:SigningKey or Jwt:KeyPath.");
    }
}
