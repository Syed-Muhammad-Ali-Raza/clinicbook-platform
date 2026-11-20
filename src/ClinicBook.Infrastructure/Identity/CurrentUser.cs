using System.Security.Claims;
using ClinicBook.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ClinicBook.Infrastructure.Identity;

/// <summary>
/// Resolves the current user from the active HTTP context.
/// </summary>
public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Initializes a new instance of the <see cref="CurrentUser"/> class.</summary>
    /// <param name="httpContextAccessor">HTTP context accessor.</param>
    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public Guid? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Principal?.FindFirstValue("sub");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    /// <inheritdoc />
    public string? Role => Principal?.FindFirstValue(ClaimTypes.Role) ?? Roles.FirstOrDefault();

    /// <inheritdoc />
    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
        ?? Array.Empty<string>();

    /// <inheritdoc />
    public Guid? ClinicId
    {
        get
        {
            var value = Principal?.FindFirstValue("clinic_id");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    /// <inheritdoc />
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;
}
