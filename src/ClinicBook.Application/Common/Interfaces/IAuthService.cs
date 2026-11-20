using ClinicBook.Application.Features.Auth.DTOs;
using ClinicBook.Domain.Common;

namespace ClinicBook.Application.Common.Interfaces;

/// <summary>
/// Application service for authentication and identity operations.
/// </summary>
public interface IAuthService
{
    /// <summary>Registers a new user account.</summary>
    Task<Result<AuthResultDto>> RegisterAsync(RegisterRequestDto request, string? ipAddress, string? deviceInfo, CancellationToken cancellationToken = default);

    /// <summary>Authenticates a user with credentials.</summary>
    Task<Result<AuthResultDto>> LoginAsync(LoginRequestDto request, string? ipAddress, string? deviceInfo, CancellationToken cancellationToken = default);

    /// <summary>Rotates refresh token and issues a new access token.</summary>
    Task<Result<AuthResultDto>> RefreshAsync(string refreshToken, string? ipAddress, string? deviceInfo, CancellationToken cancellationToken = default);

    /// <summary>Revokes a refresh token and logs out the current session.</summary>
    Task<Result> LogoutAsync(string? refreshToken, Guid? userId, string? ipAddress, CancellationToken cancellationToken = default);

    /// <summary>Verifies a user's email address.</summary>
    Task<Result> VerifyEmailAsync(VerifyEmailRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Initiates password reset flow.</summary>
    Task<Result> ForgotPasswordAsync(ForgotPasswordRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Resets a user's password using a reset token.</summary>
    Task<Result> ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>Gets the current authenticated user's profile.</summary>
    Task<Result<CurrentUserDto>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Generates a TOTP MFA setup payload for the current user.</summary>
    Task<Result<MfaSetupDto>> GetMfaSetupAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Enables TOTP MFA for the current user after verifying a code.</summary>
    Task<Result<IReadOnlyCollection<string>>> EnableMfaAsync(Guid userId, string code, CancellationToken cancellationToken = default);

    /// <summary>Disables TOTP MFA for the current user.</summary>
    Task<Result> DisableMfaAsync(Guid userId, string? code, CancellationToken cancellationToken = default);
}
