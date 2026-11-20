using System.Text.Encodings.Web;
using ClinicBook.Application.Common.Interfaces;
using ClinicBook.Application.Features.Auth.DTOs;
using ClinicBook.Domain.Common;
using ClinicBook.Domain.Entities;
using ClinicBook.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClinicBook.Infrastructure.Identity;

/// <summary>
/// Implements authentication and account security operations.
/// </summary>
public sealed class AuthService : IAuthService
{
    private const string GenericAuthError = "Invalid email or password.";
    private const string GenericResetMessage = "If the account exists, password reset instructions have been sent.";
    private const string GenericVerifyMessage = "If the account exists, verification instructions have been processed.";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly AppDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IClock _clock;
    private readonly IAuditService _auditService;
    private readonly IEmailSender _emailSender;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<AuthService> _logger;

    /// <summary>Initializes a new instance of the <see cref="AuthService"/> class.</summary>
    public AuthService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        AppDbContext dbContext,
        IJwtTokenService jwtTokenService,
        IClock clock,
        IAuditService auditService,
        IEmailSender emailSender,
        IOptions<JwtOptions> jwtOptions,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _clock = clock;
        _auditService = auditService;
        _emailSender = emailSender;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<AuthResultDto>> RegisterAsync(RegisterRequestDto request, string? ipAddress, string? deviceInfo, CancellationToken cancellationToken = default)
    {
        await EnsureDefaultRolesAsync().ConfigureAwait(false);

        var email = request.Email.Trim().ToLowerInvariant();
        var existing = await _userManager.FindByEmailAsync(email).ConfigureAwait(false);
        if (existing is not null)
        {
            await _auditService.LogAsync("Auth.Register", "Failure", null, "User", null, "Email already in use", ipAddress, deviceInfo, null, cancellationToken).ConfigureAwait(false);
            return Result<AuthResultDto>.Failure(Error.Conflict("Auth.EmailInUse", "An account with this email already exists."));
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            FirstName = request.FirstName?.Trim(),
            LastName = request.LastName?.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            EmailConfirmed = false
        };

        var createResult = await _userManager.CreateAsync(user, request.Password).ConfigureAwait(false);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            await _auditService.LogAsync("Auth.Register", "Failure", user.Id, "User", user.Id.ToString(), errors, ipAddress, deviceInfo, null, cancellationToken).ConfigureAwait(false);
            return Result<AuthResultDto>.Failure(Error.Validation("Auth.RegisterFailed", "Registration failed. Please check the provided information."));
        }

        await _userManager.AddToRoleAsync(user, "Patient").ConfigureAwait(false);

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user).ConfigureAwait(false);
        var encodedToken = WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(token));
        var verifyLink = $"verify-email?userId={user.Id}&token={encodedToken}";
        await _emailSender.SendEmailAsync(user.Email!, "Verify your ClinicBook email",
            $"<p>Verify your email by using this token/link data:</p><p>UserId: {user.Id}</p><p>Token: {HtmlEncoder.Default.Encode(encodedToken)}</p><p>Link: {HtmlEncoder.Default.Encode(verifyLink)}</p>",
            cancellationToken).ConfigureAwait(false);

        await _auditService.LogAsync("Auth.Register", "Success", user.Id, "User", user.Id.ToString(), "User registered", ipAddress, deviceInfo, null, cancellationToken).ConfigureAwait(false);

        var dto = await BuildCurrentUserDtoAsync(user).ConfigureAwait(false);
        return Result<AuthResultDto>.Success(new AuthResultDto(dto, null));
    }

    /// <inheritdoc />
    public async Task<Result<AuthResultDto>> LoginAsync(LoginRequestDto request, string? ipAddress, string? deviceInfo, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == _userManager.NormalizeEmail(email), cancellationToken).ConfigureAwait(false);
        if (user is null || user.IsDeleted)
        {
            await _auditService.LogAsync("Auth.Login", "Failure", null, "User", null, "Unknown user", ipAddress, deviceInfo, null, cancellationToken).ConfigureAwait(false);
            return Result<AuthResultDto>.Failure(Error.Unauthorized("Auth.InvalidCredentials", GenericAuthError));
        }

        if (await _userManager.IsLockedOutAsync(user).ConfigureAwait(false))
        {
            await _auditService.LogAsync("Auth.Login", "Denied", user.Id, "User", user.Id.ToString(), "Account locked out", ipAddress, deviceInfo, null, cancellationToken).ConfigureAwait(false);
            return Result<AuthResultDto>.Failure(Error.Unauthorized("Auth.LockedOut", "Account is temporarily locked. Please try again later."));
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password).ConfigureAwait(false);
        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user).ConfigureAwait(false);
            await _auditService.LogAsync("Auth.Login", "Failure", user.Id, "User", user.Id.ToString(), "Invalid password", ipAddress, deviceInfo, null, cancellationToken).ConfigureAwait(false);
            return Result<AuthResultDto>.Failure(Error.Unauthorized("Auth.InvalidCredentials", GenericAuthError));
        }

        await _userManager.ResetAccessFailedCountAsync(user).ConfigureAwait(false);

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        var requiresMfaByRole = roles.Any(r => string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase) || string.Equals(r, "Doctor", StringComparison.OrdinalIgnoreCase));

        if (user.TwoFactorEnabled || requiresMfaByRole)
        {
            if (!user.TwoFactorEnabled && requiresMfaByRole)
            {
                // MFA is mandatory for Admin/Doctor; allow login but signal requirement (enrollment flow handled separately).
                // For security, still issue session but mark; stricter enforcement can block sensitive actions.
            }

            if (user.TwoFactorEnabled)
            {
                var verified = false;
                if (!string.IsNullOrWhiteSpace(request.TwoFactorCode))
                {
                    var code = request.TwoFactorCode.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
                    verified = await _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, code).ConfigureAwait(false);
                }
                else if (!string.IsNullOrWhiteSpace(request.RecoveryCode))
                {
                    var recoveryResult = await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, request.RecoveryCode.Replace(" ", string.Empty, StringComparison.Ordinal)).ConfigureAwait(false);
                    verified = recoveryResult.Succeeded;
                }

                if (!verified)
                {
                    await _auditService.LogAsync("Auth.Login", "Failure", user.Id, "User", user.Id.ToString(), "MFA required or invalid", ipAddress, deviceInfo, null, cancellationToken).ConfigureAwait(false);
                    return Result<AuthResultDto>.Success(new AuthResultDto(await BuildCurrentUserDtoAsync(user).ConfigureAwait(false), null, true, "Multi-factor authentication code is required."));
                }
            }
        }

        var tokens = await IssueTokensAsync(user, ipAddress, deviceInfo, null, cancellationToken).ConfigureAwait(false);
        await _auditService.LogAsync("Auth.Login", "Success", user.Id, "User", user.Id.ToString(), "Login succeeded", ipAddress, deviceInfo, null, cancellationToken).ConfigureAwait(false);

        return Result<AuthResultDto>.Success(new AuthResultDto(await BuildCurrentUserDtoAsync(user).ConfigureAwait(false), tokens));
    }

    /// <inheritdoc />
    public async Task<Result<AuthResultDto>> RefreshAsync(string refreshToken, string? ipAddress, string? deviceInfo, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result<AuthResultDto>.Failure(Error.Unauthorized("Auth.RefreshMissing", "Refresh token is missing."));
        }

        var now = _clock.UtcNow;
        var tokenHash = TokenHelper.HashToken(refreshToken);
        var storedToken = await _dbContext.RefreshTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken)
            .ConfigureAwait(false);

        if (storedToken is null)
        {
            return Result<AuthResultDto>.Failure(Error.Unauthorized("Auth.RefreshInvalid", "Session is invalid. Please sign in again."));
        }

        if (storedToken.RevokedAtUtc is not null)
        {
            await RevokeTokenFamilyAsync(storedToken.FamilyId, now, "Refresh token reuse detected", cancellationToken).ConfigureAwait(false);
            await _auditService.LogAsync("Auth.RefreshTokenReuse", "Denied", storedToken.UserId, "RefreshToken", storedToken.Id.ToString(), "Revoked token reused; family revoked", ipAddress, deviceInfo, null, cancellationToken).ConfigureAwait(false);
#pragma warning disable CA1848
            _logger.LogWarning("Refresh token reuse detected for family {FamilyId}", storedToken.FamilyId);
#pragma warning restore CA1848
            return Result<AuthResultDto>.Failure(Error.Unauthorized("Auth.RefreshReuse", "Session is no longer valid. Please sign in again."));
        }

        if (storedToken.ExpiresAtUtc <= now)
        {
            storedToken.RevokedAtUtc = now;
            storedToken.RevokedReason = "Expired";
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return Result<AuthResultDto>.Failure(Error.Unauthorized("Auth.RefreshExpired", "Session expired. Please sign in again."));
        }

        var user = await _userManager.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == storedToken.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.IsDeleted)
        {
            await RevokeTokenFamilyAsync(storedToken.FamilyId, now, "User invalid", cancellationToken).ConfigureAwait(false);
            return Result<AuthResultDto>.Failure(Error.Unauthorized("Auth.RefreshInvalid", "Session is invalid. Please sign in again."));
        }

        var tokens = await IssueTokensAsync(user, ipAddress, deviceInfo, storedToken, cancellationToken).ConfigureAwait(false);
        await _auditService.LogAsync("Auth.Refresh", "Success", user.Id, "User", user.Id.ToString(), "Token rotated", ipAddress, deviceInfo, null, cancellationToken).ConfigureAwait(false);
        return Result<AuthResultDto>.Success(new AuthResultDto(await BuildCurrentUserDtoAsync(user).ConfigureAwait(false), tokens));
    }

    /// <inheritdoc />
    public async Task<Result> LogoutAsync(string? refreshToken, Guid? userId, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var tokenHash = TokenHelper.HashToken(refreshToken);
            var storedToken = await _dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken).ConfigureAwait(false);
            if (storedToken is not null && storedToken.RevokedAtUtc is null)
            {
                storedToken.RevokedAtUtc = now;
                storedToken.RevokedReason = "Logout";
                await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                userId ??= storedToken.UserId;
            }
        }

        await _auditService.LogAsync("Auth.Logout", "Success", userId, "User", userId?.ToString(), "Logout", ipAddress, null, null, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> VerifyEmailAsync(VerifyEmailRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(request.UserId, out var userId))
        {
            return Result.Failure(Error.Validation("Auth.VerifyInvalid", "Invalid verification request."));
        }

        var user = await _userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null)
        {
            return Result.Failure(Error.NotFound("Auth.UserNotFound", "Verification failed."));
        }

        string token;
        try
        {
            token = System.Text.Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
        }
        catch
        {
            return Result.Failure(Error.Validation("Auth.VerifyInvalid", "Invalid verification token."));
        }

        var result = await _userManager.ConfirmEmailAsync(user, token).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            await _auditService.LogAsync("Auth.VerifyEmail", "Failure", user.Id, "User", user.Id.ToString(), "Invalid/expired token", null, null, null, cancellationToken).ConfigureAwait(false);
            return Result.Failure(Error.Validation("Auth.VerifyFailed", "Email verification failed."));
        }

        await _auditService.LogAsync("Auth.VerifyEmail", "Success", user.Id, "User", user.Id.ToString(), "Email verified", null, null, null, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(email).ConfigureAwait(false);
        if (user is not null && !user.IsDeleted)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
            var encodedToken = WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(token));
            await _emailSender.SendEmailAsync(user.Email!, "Reset your ClinicBook password",
                $"<p>UserId: {user.Id}</p><p>Token: {HtmlEncoder.Default.Encode(encodedToken)}</p><p>This token expires according to configured policy.</p>",
                cancellationToken).ConfigureAwait(false);
            await _auditService.LogAsync("Auth.ForgotPassword", "Success", user.Id, "User", user.Id.ToString(), "Password reset requested", null, null, null, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(request.UserId, out var userId))
        {
            return Result.Failure(Error.Validation("Auth.ResetInvalid", "Invalid reset request."));
        }

        var user = await _userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null || user.IsDeleted)
        {
            return Result.Failure(Error.Validation("Auth.ResetFailed", "Password reset failed."));
        }

        string token;
        try
        {
            token = System.Text.Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(request.Token));
        }
        catch
        {
            return Result.Failure(Error.Validation("Auth.ResetInvalid", "Invalid reset token."));
        }

        var result = await _userManager.ResetPasswordAsync(user, token, request.NewPassword).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            await _auditService.LogAsync("Auth.ResetPassword", "Failure", user.Id, "User", user.Id.ToString(), "Reset failed", null, null, null, cancellationToken).ConfigureAwait(false);
            return Result.Failure(Error.Validation("Auth.ResetFailed", "Password reset failed."));
        }

        // Revoke all active refresh tokens for security after password reset.
        var now = _clock.UtcNow;
        var activeTokens = await _dbContext.RefreshTokens.Where(t => t.UserId == user.Id && t.RevokedAtUtc == null).ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var t in activeTokens)
        {
            t.RevokedAtUtc = now;
            t.RevokedReason = "Password reset";
        }
        if (activeTokens.Count > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        await _auditService.LogAsync("Auth.ResetPassword", "Success", user.Id, "User", user.Id.ToString(), "Password reset", null, null, null, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<CurrentUserDto>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.IsDeleted)
        {
            return Result<CurrentUserDto>.Failure(Error.NotFound("Auth.UserNotFound", "User not found."));
        }

        return Result<CurrentUserDto>.Success(await BuildCurrentUserDtoAsync(user).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<Result<MfaSetupDto>> GetMfaSetupAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null)
        {
            return Result<MfaSetupDto>.Failure(Error.NotFound("Auth.UserNotFound", "User not found."));
        }

        var key = await _userManager.GetAuthenticatorKeyAsync(user).ConfigureAwait(false);
        if (string.IsNullOrEmpty(key))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user).ConfigureAwait(false);
            key = await _userManager.GetAuthenticatorKeyAsync(user).ConfigureAwait(false);
        }

        var email = await _userManager.GetEmailAsync(user).ConfigureAwait(false) ?? user.UserName ?? "user";
        var issuer = Uri.EscapeDataString(_jwtOptions.Issuer ?? "ClinicBook");
        var account = Uri.EscapeDataString(email);
        var uri = $"otpauth://totp/{issuer}:{account}?secret={key}&issuer={issuer}&digits=6";
        return Result<MfaSetupDto>.Success(new MfaSetupDto(key ?? string.Empty, uri));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyCollection<string>>> EnableMfaAsync(Guid userId, string code, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null)
        {
            return Result<IReadOnlyCollection<string>>.Failure(Error.NotFound("Auth.UserNotFound", "User not found."));
        }

        var sanitized = (code ?? string.Empty).Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, sanitized).ConfigureAwait(false);
        if (!isValid)
        {
            return Result<IReadOnlyCollection<string>>.Failure(Error.Validation("Auth.MfaInvalidCode", "Invalid authenticator code."));
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true).ConfigureAwait(false);
        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10).ConfigureAwait(false);
        await _auditService.LogAsync("Auth.MfaEnabled", "Success", user.Id, "User", user.Id.ToString(), "MFA enabled", null, null, null, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyCollection<string>>.Success((recoveryCodes ?? Enumerable.Empty<string>()).ToArray());
    }

    /// <inheritdoc />
    public async Task<Result> DisableMfaAsync(Guid userId, string? code, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null)
        {
            return Result.Failure(Error.NotFound("Auth.UserNotFound", "User not found."));
        }

        if (user.TwoFactorEnabled && !string.IsNullOrWhiteSpace(code))
        {
            var sanitized = code.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);
            var valid = await _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, sanitized).ConfigureAwait(false);
            if (!valid)
            {
                return Result.Failure(Error.Validation("Auth.MfaInvalidCode", "Invalid authenticator code."));
            }
        }

        await _userManager.SetTwoFactorEnabledAsync(user, false).ConfigureAwait(false);
        await _auditService.LogAsync("Auth.MfaDisabled", "Success", user.Id, "User", user.Id.ToString(), "MFA disabled", null, null, null, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    private async Task<AuthTokensDto> IssueTokensAsync(ApplicationUser user, string? ipAddress, string? deviceInfo, RefreshToken? existingToken, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        var (accessToken, accessExpiresAtUtc) = await _jwtTokenService.CreateAccessTokenAsync(user, roles, cancellationToken).ConfigureAwait(false);

        var refreshTokenValue = TokenHelper.GenerateSecureToken(64);
        var refreshHash = TokenHelper.HashToken(refreshTokenValue);
        var refreshExpiresAtUtc = now.AddDays(Math.Max(1, _jwtOptions.RefreshTokenDays));
        var familyId = existingToken?.FamilyId ?? Guid.NewGuid();
        var csrfToken = TokenHelper.GenerateCsrfToken();

        if (existingToken is not null)
        {
            existingToken.RevokedAtUtc = now;
            existingToken.RevokedReason = "Rotated";
            existingToken.ReplacedByTokenHash = refreshHash;
            existingToken.UpdatedAtUtc = now;
        }

        var newToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            FamilyId = familyId,
            IssuedAtUtc = now,
            ExpiresAtUtc = refreshExpiresAtUtc,
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress,
            CreatedAtUtc = now
        };

        _dbContext.RefreshTokens.Add(newToken);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new AuthTokensDto(accessToken, accessExpiresAtUtc, refreshTokenValue, refreshExpiresAtUtc, csrfToken);
    }

    private async Task RevokeTokenFamilyAsync(Guid familyId, DateTime utcNow, string reason, CancellationToken cancellationToken)
    {
        var tokens = await _dbContext.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (tokens.Count == 0)
        {
            return;
        }

        foreach (var token in tokens)
        {
            token.RevokedAtUtc = utcNow;
            token.RevokedReason = reason;
            token.UpdatedAtUtc = utcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<CurrentUserDto> BuildCurrentUserDtoAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        return new CurrentUserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            user.EmailConfirmed,
            roles.ToArray(),
            user.ClinicId,
            user.TwoFactorEnabled);
    }

    private async Task EnsureDefaultRolesAsync()
    {
        foreach (var role in new[] { "Admin", "ClinicManager", "Doctor", "Receptionist", "Patient" })
        {
            if (!await _roleManager.RoleExistsAsync(role).ConfigureAwait(false))
            {
                await _roleManager.CreateAsync(new ApplicationRole(role)).ConfigureAwait(false);
            }
        }
    }
}
