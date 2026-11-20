namespace ClinicBook.Application.Features.Auth.DTOs;

/// <summary>Registration request payload.</summary>
/// <param name="Email">User email address.</param>
/// <param name="Password">Password.</param>
/// <param name="FirstName">First name.</param>
/// <param name="LastName">Last name.</param>
/// <param name="PhoneNumber">Optional phone number.</param>
public sealed record RegisterRequestDto(string Email, string Password, string FirstName, string LastName, string? PhoneNumber);

/// <summary>Login request payload.</summary>
/// <param name="Email">User email address.</param>
/// <param name="Password">Password.</param>
/// <param name="TwoFactorCode">Optional TOTP/MFA code.</param>
/// <param name="RecoveryCode">Optional recovery code.</param>
public sealed record LoginRequestDto(string Email, string Password, string? TwoFactorCode = null, string? RecoveryCode = null);

/// <summary>Email verification request payload.</summary>
/// <param name="UserId">User identifier.</param>
/// <param name="Token">Verification token.</param>
public sealed record VerifyEmailRequestDto(string UserId, string Token);

/// <summary>Forgot password request payload.</summary>
/// <param name="Email">User email address.</param>
public sealed record ForgotPasswordRequestDto(string Email);

/// <summary>Reset password request payload.</summary>
/// <param name="UserId">User identifier.</param>
/// <param name="Token">Reset token.</param>
/// <param name="NewPassword">New password.</param>
public sealed record ResetPasswordRequestDto(string UserId, string Token, string NewPassword);

/// <summary>Represents the authenticated/current user.</summary>
/// <param name="Id">User identifier.</param>
/// <param name="Email">Email address.</param>
/// <param name="FirstName">First name.</param>
/// <param name="LastName">Last name.</param>
/// <param name="EmailConfirmed">Whether email is verified.</param>
/// <param name="Roles">Assigned roles.</param>
/// <param name="ClinicId">Scoped clinic identifier, if any.</param>
/// <param name="MfaEnabled">Whether MFA is enabled.</param>
public sealed record CurrentUserDto(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    bool EmailConfirmed,
    IReadOnlyCollection<string> Roles,
    Guid? ClinicId,
    bool MfaEnabled);

/// <summary>Authentication tokens issued by the system.</summary>
/// <param name="AccessToken">JWT access token.</param>
/// <param name="AccessTokenExpiresAtUtc">Access token expiry.</param>
/// <param name="RefreshToken">Opaque refresh token.</param>
/// <param name="RefreshTokenExpiresAtUtc">Refresh token expiry.</param>
/// <param name="CsrfToken">CSRF token for double-submit protection.</param>
public sealed record AuthTokensDto(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    string CsrfToken);

/// <summary>Result of an authentication operation.</summary>
/// <param name="User">Authenticated user info.</param>
/// <param name="Tokens">Issued tokens/cookies material.</param>
/// <param name="RequiresMfa">Indicates MFA challenge is required.</param>
/// <param name="MfaRequiredReason">Reason MFA is required, if applicable.</param>
public sealed record AuthResultDto(
    CurrentUserDto? User,
    AuthTokensDto? Tokens,
    bool RequiresMfa = false,
    string? MfaRequiredReason = null);

/// <summary>MFA setup payload for TOTP authenticator apps.</summary>
/// <param name="SharedKey">Manual shared key.</param>
/// <param name="AuthenticatorUri">otpauth URI for QR provisioning.</param>
public sealed record MfaSetupDto(string SharedKey, string AuthenticatorUri);

/// <summary>Request to enable MFA.</summary>
/// <param name="Code">TOTP verification code.</param>
public sealed record EnableMfaRequestDto(string Code);

/// <summary>Request to disable MFA.</summary>
/// <param name="Code">Optional TOTP/recovery verification code.</param>
public sealed record DisableMfaRequestDto(string? Code);
