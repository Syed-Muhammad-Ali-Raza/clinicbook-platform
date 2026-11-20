using System.Security.Claims;
using ClinicBook.Application.Common.Interfaces;
using ClinicBook.Application.Features.Auth.Commands.ForgotPassword;
using ClinicBook.Application.Features.Auth.Commands.Login;
using ClinicBook.Application.Features.Auth.Commands.Logout;
using ClinicBook.Application.Features.Auth.Commands.Mfa;
using ClinicBook.Application.Features.Auth.Commands.Refresh;
using ClinicBook.Application.Features.Auth.Commands.Register;
using ClinicBook.Application.Features.Auth.Commands.ResetPassword;
using ClinicBook.Application.Features.Auth.Commands.VerifyEmail;
using ClinicBook.Application.Features.Auth.DTOs;
using ClinicBook.Application.Features.Auth.Queries.GetCurrentUser;
using ClinicBook.Domain.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ClinicBook.Api.Controllers;

/// <summary>
/// Authentication and session endpoints.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAuthCookieManager _cookieManager;

    /// <summary>Initializes a new instance of the <see cref="AuthController"/> class.</summary>
    public AuthController(IMediator mediator, IAuthCookieManager cookieManager)
    {
        _mediator = mediator;
        _cookieManager = cookieManager;
    }

    /// <summary>Issues/refreshes a CSRF token cookie for double-submit protection.</summary>
    [HttpGet("csrf")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetCsrfToken()
    {
        // Ensure CSRF cookie exists for clients that need to perform state-changing requests.
        var existing = _cookieManager.GetCsrfCookieToken();
        if (string.IsNullOrWhiteSpace(existing))
        {
            existing = InfrastructureTokenFallback();
            var secure = !HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
                         || HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue("AuthCookies:Secure", true);
            if (HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
            {
                secure = HttpContext.RequestServices.GetRequiredService<IConfiguration>().GetValue("AuthCookies:Secure", false);
            }

            Response.Cookies.Append("cb_csrf", existing, new CookieOptions
            {
                HttpOnly = false,
                Secure = secure,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                IsEssential = true
            });
        }

        return Ok(new { csrfToken = existing });
    }

    /// <summary>Registers a new user account.</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-strict")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(request.Email, request.Password, request.FirstName, request.LastName, request.PhoneNumber, GetIpAddress(), GetUserAgent());
        var result = await _mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, created: true, onSuccess: r => new { user = r.User, message = "Registration successful. Please verify your email before your first booking." });
    }

    /// <summary>Authenticates a user and sets auth cookies.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-strict")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password, request.TwoFactorCode, request.RecoveryCode, GetIpAddress(), GetUserAgent());
        var result = await _mediator.Send(command, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            return ToActionResult(result);
        }

        if (result.Value!.RequiresMfa)
        {
            return Ok(new { requiresMfa = true, reason = result.Value.MfaRequiredReason, user = result.Value.User });
        }

        ApplyAuthCookies(result.Value);
        return Ok(new { user = result.Value.User, requiresMfa = false });
    }

    /// <summary>Rotates the refresh token and issues a new access token.</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-refresh")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = _cookieManager.GetRefreshToken();
        var command = new RefreshTokenCommand(refreshToken, GetIpAddress(), GetUserAgent());
        var result = await _mediator.Send(command, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            _cookieManager.ClearAuthCookies();
            return ToActionResult(result);
        }

        ApplyAuthCookies(result.Value!);
        return Ok(new { user = result.Value!.User });
    }

    /// <summary>Logs out the current session and clears auth cookies.</summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var refreshToken = _cookieManager.GetRefreshToken();
        var userId = GetCurrentUserId();
        var command = new LogoutCommand(refreshToken, userId, GetIpAddress());
        await _mediator.Send(command, cancellationToken).ConfigureAwait(false);
        _cookieManager.ClearAuthCookies();
        return NoContent();
    }

    /// <summary>Verifies a user's email address.</summary>
    [HttpPost("verify-email")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-strict")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new VerifyEmailCommand(request.UserId, request.Token), cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    /// <summary>Initiates password reset flow.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-strict")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ForgotPasswordCommand(request.Email), cancellationToken).ConfigureAwait(false);
        return Accepted(new { message = "If the account exists, password reset instructions have been sent." });
    }

    /// <summary>Resets a user's password.</summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-strict")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ResetPasswordCommand(request.UserId, request.Token, request.NewPassword), cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    /// <summary>Gets the current authenticated user.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return UnauthorizedProblem("Authentication is required.");
        }

        var result = await _mediator.Send(new GetCurrentUserQuery(userId.Value), cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, onSuccess: user => new { user });
    }

    /// <summary>Gets MFA setup data for the current user.</summary>
    [HttpGet("mfa/setup")]
    [Authorize]
    [ProducesResponseType(typeof(MfaSetupDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMfaSetup(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return UnauthorizedProblem("Authentication is required.");
        }

        var result = await _mediator.Send(new GetMfaSetupCommand(userId.Value), cancellationToken).ConfigureAwait(false);
        return ToActionResult(result);
    }

    /// <summary>Enables MFA for the current user.</summary>
    [HttpPost("mfa/enable")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> EnableMfa([FromBody] EnableMfaRequestDto request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return UnauthorizedProblem("Authentication is required.");
        }

        var result = await _mediator.Send(new EnableMfaCommand(userId.Value, request.Code), cancellationToken).ConfigureAwait(false);
        return ToActionResult(result, onSuccess: codes => new { recoveryCodes = codes });
    }

    /// <summary>Disables MFA for the current user.</summary>
    [HttpPost("mfa/disable")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DisableMfa([FromBody] DisableMfaRequestDto request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return UnauthorizedProblem("Authentication is required.");
        }

        var result = await _mediator.Send(new DisableMfaCommand(userId.Value, request.Code), cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? NoContent() : ToActionResult(result);
    }

    private void ApplyAuthCookies(AuthResultDto authResult)
    {
        if (authResult.Tokens is null)
        {
            return;
        }

        var tokens = authResult.Tokens;
        _cookieManager.SetAuthCookies(tokens.AccessToken, tokens.AccessTokenExpiresAtUtc, tokens.RefreshToken, tokens.RefreshTokenExpiresAtUtc, tokens.CsrfToken);
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? GetUserAgent() => Request.Headers.UserAgent.ToString();

    private IActionResult ToActionResult(Result result)
    {
        if (result.IsSuccess)
        {
            return NoContent();
        }

        return ProblemFromError(result.Error!);
    }

    private IActionResult ToActionResult<T>(Result<T> result, bool created = false, Func<T, object>? onSuccess = null)
    {
        if (result.IsSuccess)
        {
            var payload = onSuccess is not null ? onSuccess(result.Value!) : result.Value;
            return created ? StatusCode(StatusCodes.Status201Created, payload) : Ok(payload);
        }

        return ProblemFromError(result.Error!);
    }

    private ObjectResult ProblemFromError(Error error)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = error.Code,
            Detail = error.Message,
            Instance = HttpContext.Request.Path
        };
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return StatusCode(status, problem);
    }

    private ObjectResult UnauthorizedProblem(string detail)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Detail = detail,
            Instance = HttpContext.Request.Path
        };
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return StatusCode(StatusCodes.Status401Unauthorized, problem);
    }

    private static string InfrastructureTokenFallback() => Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Replace("+", "-", StringComparison.Ordinal).Replace("/", "_", StringComparison.Ordinal).TrimEnd('=');
}
